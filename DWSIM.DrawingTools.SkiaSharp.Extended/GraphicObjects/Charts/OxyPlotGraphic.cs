﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DWSIM.Drawing.SkiaSharp.GraphicObjects;
using OxyPlot;
using SkiaSharp;

namespace DWSIM.Drawing.SkiaSharp.GraphicObjects.Charts
{
    public class OxyPlotGraphic : ShapeGraphic
    {

        private Renderers.SKCanvasRenderContext renderer = new Renderers.SKCanvasRenderContext(1.0);

        #region "Constructors"

        public void Init()
        {
            this.ObjectType = DWSIM.Interfaces.Enums.GraphicObjects.ObjectType.GO_Chart;
            this.Description = "Chart Object";
            Width = 100;
            Height = 100;
        }

        public OxyPlotGraphic()
            : base()
        {
            Init();
        }

        public OxyPlotGraphic(SKPoint graphicPosition)
            : base()
        {
            Init();
            this.SetPosition(graphicPosition);
        }

        public OxyPlotGraphic(int posX, int posY)
        {
            Init();
            this.SetPosition(new SKPoint(posX, posY));
        }

        public OxyPlotGraphic(SKPoint graphicPosition, SKSize graphicSize)
            : base()
        {
            Init();
            this.SetSize(graphicSize);
            this.SetPosition(graphicPosition);
        }

        public OxyPlotGraphic(int posX, int posY, int width, int height)
            : base()
        {
            Init();
            this.SetSize(new SKSize(width, height));
            this.SetPosition(new SKPoint(posX, posY));
        }

        #endregion

        public override bool LoadData(List<System.Xml.Linq.XElement> data)
        {
            base.LoadData(data);
            return true;
        }

        public override List<System.Xml.Linq.XElement> SaveData()
        {
            return base.SaveData();
        }

        private string _ModelName = "";

        public string ModelName
        {
            get
            {
                return _ModelName;
            }
            set
            {
                _ModelName = value;
            }
        }

        private string _OwnerID = "";

        public string OwnerID
        {
            get
            {
                return _OwnerID;
            }
            set
            {
                _OwnerID = value;
            }
        }

        public Interfaces.IFlowsheet Flowsheet { get; set; }

        public override void Draw(object g)
        {

            var canvas = (SKCanvas)g;

            base.Draw(g);

            if (Flowsheet != null)
            {
                if (OwnerID != null && Flowsheet.SimulationObjects.ContainsKey(OwnerID))
                {

                    var obj = Flowsheet.SimulationObjects[OwnerID];

                    IPlotModel model = null;

                    try
                    {
                        model = (IPlotModel)(obj.GetChartModel(ModelName));
                    }
                    catch
                    {
                        PaintInstructions(canvas, "Chart model not found.");
                        return;
                    }

                    if (model != null)
                    {
                        try
                        {
                            using (var surface = SKSurface.Create(new SKImageInfo(Width, Height)))
                            {
                                renderer.SetTarget(surface.Canvas);
                                model.Update(true);
                                model.Render(renderer, Width, Height);
                                var paint = GetPaint(SKColors.Black);
                                paint.FilterQuality = SKFilterQuality.High;
                                paint.IsAutohinted = true;
                                paint.LcdRenderText = true;
                                canvas.DrawSurface(surface, X, Y, paint);
                                canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), GetStrokePaint(SKColors.Black, 1.0f));
                            }
                        }
                        catch (Exception ex)
                        {
                            PaintInstructions(canvas, "Error drawing chart: " + ex.Message.ToString());
                        }
                    }
                    else
                    {
                        PaintInstructions(canvas, "Chart model not found.");
                    }

                }
                else
                {
                    PaintInstructions(canvas, "Referenced flowsheet object not found.");
                }

            }
            else
            {
                PaintInstructions(canvas, "Flowsheet not defined.");
            }
        }

        private void PaintInstructions(SKCanvas canvas, string text)
        {

            var tpaint = GetPaint(SKColors.Black);

            var size = this.MeasureString(text, tpaint);

            var width = (int)size.Width;
            var height = (int)size.Height;

            canvas.DrawText(text, X + (Width - width) / 2, Y + (Height - height) / 2, tpaint);

            canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), GetStrokePaint(SKColors.Black, 1.0f));

        }

    }
}
