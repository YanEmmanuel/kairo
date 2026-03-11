using Kairo.Core.Models;

namespace Kairo.Terminal.Ansi;

public static class AnsiDiffRenderer
{
    public static void Render(
        TerminalFrameBuffer current,
        TerminalFrameBuffer previous,
        PooledAnsiBuilder builder,
        bool forceFullRedraw)
    {
        if (current.Width != previous.Width || current.Height != previous.Height)
        {
            throw new InvalidOperationException("Frame buffers must have the same dimensions.");
        }

        var currentCells = current.Cells;
        var previousCells = previous.Cells;
        var styleActive = false;
        var lastForeground = Rgb24.Black;
        var lastBackground = Rgb24.Black;
        var lastRow = -1;
        var lastColumn = -1;

        for (var y = 0; y < current.Height; y++)
        {
            for (var x = 0; x < current.Width; x++)
            {
                var index = (y * current.Width) + x;
                var cell = currentCells[index];

                if (!forceFullRedraw && cell == previousCells[index])
                {
                    continue;
                }

                if (lastRow != y || lastColumn != x)
                {
                    builder.AppendCursorPosition(y + 1, x + 1);
                    lastRow = y;
                    lastColumn = x;
                }

                if (!styleActive || cell.Foreground != lastForeground)
                {
                    builder.AppendForeground(cell.Foreground);
                    lastForeground = cell.Foreground;
                    styleActive = true;
                }

                if (!styleActive || cell.Background != lastBackground)
                {
                    builder.AppendBackground(cell.Background);
                    lastBackground = cell.Background;
                    styleActive = true;
                }

                builder.Append(cell.Glyph);
                lastColumn++;
            }
        }

        builder.AppendReset();
    }
}
