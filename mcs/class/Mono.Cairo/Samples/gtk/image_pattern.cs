//
//
//	Mono.Cairo drawing samples using GTK# as drawing surface
//	Autor: Jordi Mas <jordi@ximian.com>. Based on work from Owen Taylor
//	       Hisham Mardam Bey <hisham@hisham.cc>
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Cairo;
using Gtk;
	
public class GtkCairo
{
	static DrawingArea a;
	
	static void Main ()
	{		
		Application.Init ();
		Gtk.Window w = new Gtk.Window ("Mono.Cairo Circles demo");

		a = new CairoGraphic ();	
		
		Box box = new HBox (true, 0);
		box.Add (a);
		w.Add (box);
		w.Resize (500,500);		
		w.ShowAll ();		
		
		Application.Run ();
	}


}

public class CairoGraphic : DrawingArea 
{	       	
        static readonly double  M_PI = 3.14159265358979323846;
   
	static void draw (Cairo.Graphics gr, int width, int height)
	{
		int w, h;
		SurfaceImage image;
		Matrix matrix;
		Pattern pattern;
		
		gr.Scale (width, height);
		gr.LineWidth = 0.04;

		image = new SurfaceImage ("data/e.png");
		w = image.Width;
		h = image.Height;
		
		pattern = new Pattern (image);
		pattern.Extend = Cairo.Extend.Repeat;
		
		gr.Translate (0.5, 0.5);
		gr.Rotate (M_PI / 4);
		gr.Scale (1 / Math.Sqrt (2), 1 / Math.Sqrt (2));
		gr.Translate (- 0.5, - 0.5);
		
		matrix = new Matrix ();
		matrix.InitScale (w * 5.0, h * 5.0);
		
		pattern.Matrix = matrix;
		
		gr.Pattern = pattern;
		
		gr.Rectangle ( new PointD (0, 0),
			       1.0, 1.0);
		gr.Fill ();
		
		pattern.Destroy ();
		image.Destroy();
	}
   
	
	protected override bool OnExposeEvent (Gdk.EventExpose args)
	{
		Gdk.Window win = args.Window;
		//Gdk.Rectangle area = args.Area;
		
		Cairo.Graphics g = Gdk.Graphics.CreateDrawable (win);
		
		int x, y, w, h, d;
		win.GetGeometry(out x, out y, out w, out h, out d);
		
		draw (g, w, h);
		return true;
	}

}

