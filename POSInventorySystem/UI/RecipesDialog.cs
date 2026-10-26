using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// Read-only view of what each drink uses, in real units, and how many of
// it the current ingredient stock can still make.
public class RecipesDialog : Form
{
    public RecipesDialog()
    {
        Text = "Recipes";
        ClientSize = new Size(760, 560);
        MinimumSize = new Size(680, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Theme.GreenDark
        };

        Label title = Theme.Label("Recipes", 15f, true, Color.White);
        title.Location = new Point(22, 10);

        Label sub = Theme.Label(
            "What each drink uses per serving (base recipe, before " +
            "milk, shot and syrup options)",
            9.5f, false, Theme.Gold);
        sub.Location = new Point(24, 40);

        header.Controls.AddRange(new Control[] { title, sub });

        DataGridView grid = new DataGridView { Dock = DockStyle.Fill };
        Theme.StyleGrid(grid);
        grid.Columns.Add("Drink", "Drink");
        grid.Columns.Add("Ingredient", "Ingredient");
        grid.Columns.Add("Per", "Per serving");
        grid.Columns.Add("Stock", "In stock");
        grid.Columns[0].FillWeight = 130;
        grid.Columns[1].FillWeight = 120;
        grid.Columns[2].FillWeight = 80;
        grid.Columns[3].FillWeight = 130;

        foreach (Recipe recipe in RecipeData.Recipes)
        {
            bool first = true;
            foreach (RecipeIngredient line in recipe.Ingredients)
            {
                Ingredient i = line.Ingredient;

                decimal perServing = line.QuantityRequired * i.PackSize;

                int row = grid.Rows.Add(
                    first ? recipe.Product.Name : "",
                    i.Name,
                    i.Unit.Length == 0
                        ? $"{line.QuantityRequired:0.###} packs"
                        : $"{perServing:#,0.##} {i.Unit}",
                    Ingredient.Packs(i.Quantity));

                if (first)
                {
                    grid.Rows[row].DefaultCellStyle.Font = Theme.Bold(10f);
                }

                first = false;
            }
        }

        Panel host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 0)
        };

        host.Controls.Add(grid);

        Panel footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64
        };

        RoundedButton close = Theme.PrimaryButton("Close");
        close.Size = new Size(140, 42);
        close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        close.Location = new Point(footer.Width - 160, 10);
        close.Click += (_, _) => Close();

        footer.Controls.Add(close);
        footer.Resize += (_, _) =>
            close.Location = new Point(footer.Width - close.Width - 20, 10);

        Controls.Add(host);
        Controls.Add(footer);
        Controls.Add(header);

        CancelButton = close;
    }
}
