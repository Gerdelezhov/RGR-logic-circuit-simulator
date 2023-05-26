using Avalonia;

namespace RGR.Views.Shapes {
    internal interface IGate {
        public Point GetPos();
        public void Move(Point pos);
        public void Resize(Size size);
    }
}
