﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Reflection;

namespace System.ServiceModel.Dispatcher
{
	internal class OperationInvokerHandler : BaseRequestProcessorHandler
	{
		IDuplexChannel duplex;

		public OperationInvokerHandler (IChannel channel)
		{
			duplex = channel as IDuplexChannel;
		}

		protected override bool ProcessRequest (MessageProcessingContext mrc)
		{			
			RequestContext rc = mrc.RequestContext;
			DispatchRuntime dispatchRuntime = mrc.OperationContext.EndpointDispatcher.DispatchRuntime;
			DispatchOperation operation = GetOperation (mrc.IncomingMessage, dispatchRuntime);
			mrc.Operation = operation;
			try {				
				DoProcessRequest (mrc);
				if (!operation.Invoker.IsSynchronous)
					return true;
				if (!mrc.Operation.IsOneWay)
					Reply (mrc, true);
			} catch (TargetInvocationException ex) {
				mrc.ReplyMessage = BuildExceptionMessage (mrc, ex.InnerException, 
					dispatchRuntime.ChannelDispatcher.IncludeExceptionDetailInFaults);
				if (!mrc.Operation.IsOneWay)
					Reply (mrc, true);
				ProcessCustomErrorHandlers (mrc, ex);
			}
			return false;
		}

		void DoProcessRequest (MessageProcessingContext mrc)
		{
			DispatchOperation operation = mrc.Operation;
			Message req = mrc.IncomingMessage;
			object instance = mrc.InstanceContext.GetServiceInstance(req);
			object [] parameters, outParams;
			BuildInvokeParams (mrc, out parameters);

			if (operation.Invoker.IsSynchronous) {
				object result = operation.Invoker.Invoke (instance, parameters, out outParams);
				HandleInvokeResult (mrc, outParams, result);
			} else {// asynchronous
				InvokeAsynchronous (mrc, instance, parameters);
			}			
		}

		void InvokeAsynchronous (MessageProcessingContext mrc, object instance, object [] parameters)
		{
			DispatchOperation operation = mrc.Operation;
			DispatchRuntime dispatchRuntime = mrc.OperationContext.EndpointDispatcher.DispatchRuntime;
			operation.Invoker.InvokeBegin (instance, parameters,
					delegate (IAsyncResult res) {						
						try {
							object result;
							result = operation.Invoker.InvokeEnd (instance, out parameters, res);
							HandleInvokeResult (mrc, parameters, result);
							Reply (mrc, true);
						} catch (Exception ex) {
							mrc.ReplyMessage = BuildExceptionMessage (mrc, ex, dispatchRuntime.ChannelDispatcher.IncludeExceptionDetailInFaults);
							if (!mrc.Operation.IsOneWay)
								Reply (mrc, false);
							ProcessCustomErrorHandlers (mrc, ex);
						}				
					},
					null);			
		}

		void Reply (MessageProcessingContext mrc, bool useTimeout)
		{
			if (duplex != null)
				mrc.Reply (duplex, useTimeout);
			else
				mrc.Reply (useTimeout);
		}

		DispatchOperation GetOperation (Message input, DispatchRuntime dispatchRuntime)
		{
			if (dispatchRuntime.OperationSelector != null) {
				string name = dispatchRuntime.OperationSelector.SelectOperation (ref input);
				foreach (DispatchOperation d in dispatchRuntime.Operations)
					if (d.Name == name)
						return d;
			} else {
				string action = input.Headers.Action;
				foreach (DispatchOperation d in dispatchRuntime.Operations)
					if (d.Action == action)
						return d;
			}
			return dispatchRuntime.UnhandledDispatchOperation;
		}

		void HandleInvokeResult (MessageProcessingContext mrc, object [] outputs, object result)
		{
			DispatchOperation operation = mrc.Operation;
			mrc.EventsHandler.AfterInvoke (operation);

			if (operation.IsOneWay)
				return;

			Message res = null;
			if (operation.SerializeReply)
				res = operation.Formatter.SerializeReply (
					mrc.OperationContext.EndpointDispatcher.ChannelDispatcher.MessageVersion, outputs, result);
			else
				res = (Message) result;
			res.Headers.CopyHeadersFrom (mrc.OperationContext.OutgoingMessageHeaders);
			res.Properties.CopyProperties (mrc.OperationContext.OutgoingMessageProperties);
			mrc.ReplyMessage = res;
		}

		Message CreateActionNotSupported (Message req)
		{
			FaultCode fc = new FaultCode (
				req.Version.Addressing.ActionNotSupported,
				req.Version.Addressing.Namespace);
			// FIXME: set correct namespace URI
			return Message.CreateMessage (req.Version, fc,
				String.Format ("action '{0}' is not supported in this service contract.", req.Headers.Action), String.Empty);
		}

		void BuildInvokeParams (MessageProcessingContext mrc, out object [] parameters)
		{
			DispatchOperation operation = mrc.Operation;
			EnsureValid (operation);

			if (operation.DeserializeRequest) {
				parameters = operation.Invoker.AllocateInputs ();
				operation.Formatter.DeserializeRequest (mrc.IncomingMessage, parameters);
			} else
				parameters = new object [] { mrc.IncomingMessage };

			mrc.EventsHandler.BeforeInvoke (operation);
		}

		void ProcessCustomErrorHandlers (MessageProcessingContext mrc, Exception ex)
		{
			var dr = mrc.OperationContext.EndpointDispatcher.DispatchRuntime;
			bool shutdown = false;
			foreach (var eh in dr.ChannelDispatcher.ErrorHandlers)
				shutdown |= eh.HandleError (ex);
			if (shutdown)
				ProcessSessionErrorShutdown (mrc);
		}

		void ProcessSessionErrorShutdown (MessageProcessingContext mrc)
		{
			var dr = mrc.OperationContext.EndpointDispatcher.DispatchRuntime;
			var session = mrc.OperationContext.Channel.InputSession;
			var dcc = mrc.OperationContext.Channel as IDuplexContextChannel;
			if (session == null || dcc == null)
				return;
			foreach (var h in dr.InputSessionShutdownHandlers)
				h.ChannelFaulted (dcc);
		}

		Message BuildExceptionMessage (MessageProcessingContext mrc, Exception ex, bool includeDetailsInFault)
		{
			var dr = mrc.OperationContext.EndpointDispatcher.DispatchRuntime;
			var cd = dr.ChannelDispatcher;
			Message msg = null;
			foreach (var eh in cd.ErrorHandlers)
				eh.ProvideFault (ex, cd.MessageVersion, ref msg);
			if (msg != null)
				return msg;

			var req = mrc.IncomingMessage;

			// FIXME: set correct name
			FaultCode fc = new FaultCode (
				"InternalServiceFault",
				req.Version.Addressing.Namespace);


			if (includeDetailsInFault) {
				return Message.CreateMessage (req.Version, fc, ex.Message, new ExceptionDetail (ex), req.Headers.Action);
			}

			string faultString =
				@"The server was unable to process the request due to an internal error.  The server may be able to return exception details (it depends on the server settings).";
			return Message.CreateMessage (req.Version, fc, faultString, req.Headers.Action);
		}

		void EnsureValid (DispatchOperation operation)
		{
			if (operation.Invoker == null)
				throw new InvalidOperationException ("DispatchOperation requires Invoker.");
			if ((operation.DeserializeRequest || operation.SerializeReply) && operation.Formatter == null)
				throw new InvalidOperationException ("The DispatchOperation '" + operation.Name + "' requires Formatter, since DeserializeRequest and SerializeReply are not both false.");
		}		
	}
}
