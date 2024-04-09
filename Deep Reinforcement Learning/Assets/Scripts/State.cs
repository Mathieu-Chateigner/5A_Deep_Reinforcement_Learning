using System;

public class State
{
    public int X { get; set; }
    public int Y { get; set; }

    public State(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object obj)
    {
        return obj is State state && X == state.X && Y == state.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"State(x={X}, y={Y})";
    }
}