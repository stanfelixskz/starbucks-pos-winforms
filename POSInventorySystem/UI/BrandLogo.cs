namespace POSInventorySystem.UI;

// Loads Assets\logo.png (if present) and shows it scaled to fit.
// When the file is missing, callers keep their text header.
public static class BrandLogo
{
    private static Image? cached;
    private static bool looked;

    // Accepts logo.png or logo.jpg, whichever is present
    private static string? FindFile()
    {
        foreach (string name in new[] { "logo.png", "logo.jpg" })
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", name);

            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public static bool Exists => FindFile() != null;

    public static Image? Image
    {
        get
        {
            if (!looked)
            {
                looked = true;

                string? path = FindFile();

                if (path != null)
                {
                    try
                    {
                        using FileStream stream = File.OpenRead(path);
                        cached = Image.FromStream(stream);
                    }
                    catch (Exception)
                    {
                        cached = null;
                    }
                }
            }

            return cached;
        }
    }

    // A picture box sized to fit inside maxWidth x maxHeight, keeping proportions.
    // Returns null when there is no logo.
    public static PictureBox? Create(int maxWidth, int maxHeight)
    {
        Image? image = Image;

        if (image == null)
        {
            return null;
        }

        double scale = Math.Min(
            (double)maxWidth / image.Width,
            (double)maxHeight / image.Height);

        return new PictureBox
        {
            Image = image,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(
                (int)(image.Width * scale),
                (int)(image.Height * scale)),
            BackColor = Color.Transparent
        };
    }
}
