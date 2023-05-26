using Avalonia;
using Avalonia.Controls;

namespace RGR.Views.Shapes {
    public interface IGate {
        public UserControl GetSelf();
        public Point GetPos();
        public Size GetSize();
        public void Move(Point pos);
        public void Resize(Size size);
    }
}
