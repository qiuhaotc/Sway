using System.Windows;
using Point = System.Windows.Point;

namespace Sway;

public interface IMouseService
{
    Point GetMousePosition();
    void MoveMouse(int x, int y);
}
