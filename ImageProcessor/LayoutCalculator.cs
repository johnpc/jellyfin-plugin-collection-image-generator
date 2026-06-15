using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.CollectionImageGenerator.ImageProcessor
{
    /// <summary>
    /// Calculates image positions for various collage layouts.
    /// </summary>
    internal static class LayoutCalculator
    {
        /// <summary>
        /// Gets custom positions for each image based on the layout type with consistent padding.
        /// </summary>
        /// <param name="count">The number of images to position.</param>
        /// <param name="canvasWidth">The canvas width in pixels.</param>
        /// <param name="canvasHeight">The canvas height in pixels.</param>
        /// <param name="padding">The padding between images in pixels.</param>
        /// <returns>A list of position tuples (X, Y, Width, Height) for each image.</returns>
        internal static List<(int X, int Y, int Width, int Height)> GetCustomPositions(int count, int canvasWidth, int canvasHeight, int padding)
        {
            return count switch
            {
                1 => GetSingleImageLayout(canvasWidth, canvasHeight, padding),
                2 => GetDiagonalLayout(canvasWidth, canvasHeight, padding),
                3 => GetTriangularLayout(canvasWidth, canvasHeight, padding),
                4 => GetQuadLayout(canvasWidth, canvasHeight, padding),
                5 => GetLayout_2_1_2(canvasWidth, canvasHeight, padding),
                6 => GetLayout_2_2_2(canvasWidth, canvasHeight, padding),
                7 => GetLayout_2_3_2(canvasWidth, canvasHeight, padding),
                8 => GetLayout_3_2_3(canvasWidth, canvasHeight, padding),
                _ => GetStandardGridLayout(count, canvasWidth, canvasHeight, padding)
            };
        }

        /// <summary>
        /// Calculates cell size for a 3x3 grid layout.
        /// </summary>
        /// <param name="canvasWidth">The canvas width.</param>
        /// <param name="canvasHeight">The canvas height.</param>
        /// <param name="padding">The padding between cells.</param>
        /// <returns>A tuple of (Width, Height) for each cell.</returns>
        internal static (int Width, int Height) Get3x3CellSize(int canvasWidth, int canvasHeight, int padding)
        {
            return ((canvasWidth - (padding * 4)) / 3, (canvasHeight - (padding * 4)) / 3);
        }

        /// <summary>
        /// Calculates cell size for a 2x2 grid layout.
        /// </summary>
        /// <param name="canvasWidth">The canvas width.</param>
        /// <param name="canvasHeight">The canvas height.</param>
        /// <param name="padding">The padding between cells.</param>
        /// <returns>A tuple of (Width, Height) for each cell.</returns>
        internal static (int Width, int Height) Get2x2CellSize(int canvasWidth, int canvasHeight, int padding)
        {
            return ((canvasWidth - (padding * 3)) / 2, (canvasHeight - (padding * 3)) / 2);
        }

        private static void AddStandardRow(
            List<(int X, int Y, int Width, int Height)> positions,
            int rowIndex,
            int startCol,
            int itemCount,
            int cellWidth,
            int cellHeight,
            int padding)
        {
            var rowY = padding + (rowIndex * (cellHeight + padding));
            for (var i = 0; i < itemCount; i++)
            {
                var colX = padding + ((startCol + i) * (cellWidth + padding));
                positions.Add((colX, rowY, cellWidth, cellHeight));
            }
        }

        private static void AddCenteredRow(
            List<(int X, int Y, int Width, int Height)> positions,
            int rowIndex,
            int itemCount,
            int cellWidth,
            int cellHeight,
            int padding,
            int canvasWidth)
        {
            var rowY = padding + (rowIndex * (cellHeight + padding));
            var centerOffset = (cellWidth + padding) / 2;
            var startX = padding + centerOffset;

            for (var i = 0; i < itemCount; i++)
            {
                var colX = startX + (i * (cellWidth + padding));
                positions.Add((colX, rowY, cellWidth, cellHeight));
            }
        }

        private static List<(int X, int Y, int Width, int Height)> GetSingleImageLayout(int canvasWidth, int canvasHeight, int padding)
        {
            return new List<(int X, int Y, int Width, int Height)>
            {
                (padding, padding, canvasWidth - (padding * 2), canvasHeight - (padding * 2)),
            };
        }

        private static List<(int X, int Y, int Width, int Height)> GetDiagonalLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var eighthWidth = canvasWidth / 8;

            return new List<(int X, int Y, int Width, int Height)>
            {
                (padding + eighthWidth, padding, width, height),
                (canvasWidth - width - padding - eighthWidth, canvasHeight - height - padding, width, height),
            };
        }

        private static List<(int X, int Y, int Width, int Height)> GetTriangularLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var topCenterX = (canvasWidth - width) / 2;

            return new List<(int X, int Y, int Width, int Height)>
            {
                (topCenterX, padding, width, height),
                (padding, (padding * 2) + height, width, height),
                ((padding * 2) + width, (padding * 2) + height, width, height),
            };
        }

        private static List<(int X, int Y, int Width, int Height)> GetQuadLayout(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get2x2CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddStandardRow(positions, 0, 0, 2, width, height, padding);
            AddStandardRow(positions, 1, 0, 2, width, height, padding);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_1_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);

            var centerX = (canvasWidth - width) / 2;
            var middleY = (padding * 2) + height;
            positions.Add((centerX, middleY, width, height));

            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_2_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);

            var middleRowSpacing = (int)(width * 0.4f);
            var middleStartX = (canvasWidth - ((width * 2) + middleRowSpacing)) / 2;
            var middleY = (padding * 2) + height;
            positions.Add((middleStartX, middleY, width, height));
            positions.Add((middleStartX + width + middleRowSpacing, middleY, width, height));

            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetLayout_2_3_2(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddCenteredRow(positions, 0, 2, width, height, padding, canvasWidth);
            AddStandardRow(positions, 1, 0, 3, width, height, padding);
            AddCenteredRow(positions, 2, 2, width, height, padding, canvasWidth);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetLayout_3_2_3(int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            AddStandardRow(positions, 0, 0, 3, width, height, padding);
            AddCenteredRow(positions, 1, 2, width, height, padding, canvasWidth);
            AddStandardRow(positions, 2, 0, 3, width, height, padding);

            return positions;
        }

        private static List<(int X, int Y, int Width, int Height)> GetStandardGridLayout(int count, int canvasWidth, int canvasHeight, int padding)
        {
            var (width, height) = Get3x3CellSize(canvasWidth, canvasHeight, padding);
            var positions = new List<(int X, int Y, int Width, int Height)>();

            for (var i = 0; i < Math.Min(count, 9); i++)
            {
                var row = i / 3;
                var col = i % 3;
                var x = padding + (col * (width + padding));
                var y = padding + (row * (height + padding));
                positions.Add((x, y, width, height));
            }

            return positions;
        }
    }
}
