namespace dok.image.binarization.win
{
    [System.Flags]
    public enum LoadMode
    {
        Unchanged = -1,
        GrayScale = 0,
        Color = 1,
        AnyDepth = 2,
        AnyColor = Color | AnyDepth
    }
}
