using System.Drawing;

namespace PONG.Elements {
    public class Element {

        public RectangleF eRectangle;
        public Color eColor;

        public Element(RectangleF _rectangle, Color _color)
        {
            eRectangle = _rectangle;
            eColor = _color;
        }

    }
}
