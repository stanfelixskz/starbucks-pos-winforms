using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem.UI;

// Build or edit a drink's recipe: which ingredients it uses and how much of
// each per serving, for every cup size. Amounts are in the ingredient's own
// unit (g, ml or pcs), like a recipe card.
public class RecipeDialog : Form
{
    private const int RowHeight = 54;

    private sealed class Line
    {
        public required ComboBox Ingredient { get; init; }
        public required NumericUpDown[] Amounts { get; init; }   // Tall, Grande, Venti
        public required Label Unit { get; init; }
        public required Label Hint { get; init; }
        public required Control[] Controls { get; init; }

        // Grande and Venti follow Tall until someone changes them
        public bool FollowTall { get; set; }
    }

    private readonly List<Line> lines = new();
    private readonly Panel list;
    private readonly Label errorLabel;
    private readonly RoundedButton addButton;
    private readonly List<Ingredient> ingredients;
    private bool settingValues;

    // The recipe as entered (valid once the dialog returns OK)
    public IReadOnlyList<RecipeRow> Rows { get; private set; } = new List<RecipeRow>();

    public RecipeDialog(string drinkName, IReadOnlyList<RecipeRow>? existing)
    {
        ingredients = DataStore.Ingredients.OrderBy(i => i.Name).ToList();

        Text = "Recipe";
        ClientSize = new Size(780, 640);
        MinimumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        // ---- Header ----
        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92,
            BackColor = Theme.GreenDark
        };

        Label title = Theme.Label($"Recipe for {drinkName}", 15f, true, Color.White);
        title.Location = new Point(22, 12);

        Label sub = Theme.Label(
            "Amount used per serving, in each ingredient's own unit. Grande and Venti " +
            "start at 1.25x and 1.5x of Tall; change them if your cup needs less or more.",
            9.5f, false, Theme.Gold);
        sub.AutoSize = false;
        sub.Size = new Size(720, 38);
        sub.Location = new Point(24, 46);

        header.Controls.AddRange(new Control[] { title, sub });

        // ---- Rows ----
        list = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 0)
        };

        string[] captions = { "INGREDIENT", "TALL", "GRANDE", "VENTI", "UNIT" };
        int[] captionX = { 24, 306, 408, 510, 622 };

        for (int i = 0; i < captions.Length; i++)
        {
            Label caption = Theme.Label(captions[i], 9f, true, Theme.Muted);
            caption.Location = new Point(captionX[i], 12);
            list.Controls.Add(caption);
        }

        // ---- Footer ----
        Panel footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 132
        };

        addButton = Theme.SecondaryButton("+  Add ingredient");
        addButton.Size = new Size(170, 38);
        addButton.Font = Theme.Bold(9.5f);
        addButton.Location = new Point(24, 10);
        addButton.Click += (_, _) => AddLine(null, 0, 0, 0, followTall: true);

        errorLabel = Theme.Label("", 9.5f, false, Theme.Danger);
        errorLabel.AutoSize = false;
        errorLabel.Size = new Size(720, 34);
        errorLabel.Location = new Point(24, 54);

        RoundedButton save = Theme.PrimaryButton("Save recipe");
        save.Size = new Size(200, 46);
        save.Location = new Point(24, 82);
        save.Click += (_, _) => Save();

        RoundedButton cancel = Theme.SecondaryButton("Cancel");
        cancel.Size = new Size(110, 46);
        cancel.Location = new Point(240, 82);
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.AddRange(new Control[] { addButton, errorLabel, save, cancel });

        Controls.Add(list);
        Controls.Add(footer);
        Controls.Add(header);

        CancelButton = cancel;

        // ---- Fill in an existing recipe, or start with one empty row ----
        if (existing != null && existing.Count > 0)
        {
            foreach (var group in existing.GroupBy(r => r.IngredientId))
            {
                Ingredient? ingredient = DataStore.GetIngredient(group.Key);

                if (ingredient == null)
                {
                    continue;
                }

                decimal Amount(string size) =>
                    group.FirstOrDefault(r => r.Size == size)?.Amount ?? 0m;

                AddLine(ingredient,
                    Amount("Tall"), Amount("Grande"), Amount("Venti"),
                    followTall: false);
            }
        }
        else
        {
            AddLine(null, 0, 0, 0, followTall: true);
        }

        UpdateAddButton();
    }

    // ==============================
    // ROWS
    // ==============================

    private void AddLine(
        Ingredient? ingredient,
        decimal tall,
        decimal grande,
        decimal venti,
        bool followTall)
    {
        ingredient ??= ingredients.FirstOrDefault(
            i => lines.All(l => l.Ingredient.SelectedItem != i));

        int y = 36 + lines.Count * RowHeight;

        ComboBox combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = nameof(Ingredient.Name),
            Width = 262,
            Font = Theme.Regular(11f),
            Location = new Point(24, y)
        };

        combo.Items.AddRange(ingredients.Cast<object>().ToArray());

        NumericUpDown[] amounts = new NumericUpDown[3];
        decimal[] values = { tall, grande, venti };
        int[] xs = { 306, 408, 510 };

        for (int i = 0; i < 3; i++)
        {
            amounts[i] = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100000,
                DecimalPlaces = 1,
                Increment = 1,
                Value = Math.Min(100000, values[i]),
                Width = 90,
                Font = Theme.Regular(11f),
                Location = new Point(xs[i], y)
            };
        }

        Label unit = Theme.Label("", 10f, false, Theme.Muted);
        unit.Location = new Point(622, y + 4);

        Label hint = Theme.Label("", 9f, false, Theme.Muted);
        hint.Location = new Point(24, y + 30);

        RoundedButton remove = Theme.DangerButton("Remove");
        remove.Size = new Size(76, 30);
        remove.Font = Theme.Bold(8.5f);
        remove.Location = new Point(680, y - 2);

        Control[] all = { combo, amounts[0], amounts[1], amounts[2], unit, hint, remove };

        Line line = new Line
        {
            Ingredient = combo,
            Amounts = amounts,
            Unit = unit,
            Hint = hint,
            Controls = all,
            FollowTall = followTall
        };

        lines.Add(line);
        list.Controls.AddRange(all);

        combo.SelectedItem = ingredient;
        combo.SelectedIndexChanged += (_, _) => Refresh(line);

        amounts[0].ValueChanged += (_, _) =>
        {
            if (line.FollowTall && !settingValues)
            {
                settingValues = true;
                amounts[1].Value = Scaled(amounts[0].Value, "Grande");
                amounts[2].Value = Scaled(amounts[0].Value, "Venti");
                settingValues = false;
            }

            Refresh(line);
        };

        // Once Grande or Venti is edited by hand, they stop following Tall
        foreach (NumericUpDown box in new[] { amounts[1], amounts[2] })
        {
            box.ValueChanged += (_, _) =>
            {
                if (!settingValues)
                {
                    line.FollowTall = false;
                }
            };
        }

        remove.Click += (_, _) => RemoveLine(line);

        Refresh(line);
        UpdateAddButton();
    }

    private static decimal Scaled(decimal tall, string size) =>
        Math.Min(100000, Math.Round(tall * Recipe.ScaleFor(size), 1));

    // The unit and the "how many servings from one pack" hint
    private static void Refresh(Line line)
    {
        if (line.Ingredient.SelectedItem is not Ingredient ingredient)
        {
            line.Unit.Text = "";
            line.Hint.Text = "";
            return;
        }

        line.Unit.Text = ingredient.Unit.Length > 0 ? ingredient.Unit : "units";

        decimal tall = line.Amounts[0].Value;

        line.Hint.Text = tall > 0 && ingredient.PackSize > 0
            ? $"1 pack = {ingredient.PackSize:#,0.##} {ingredient.Unit}  " +
              $"≈ {Math.Floor(ingredient.PackSize / tall):#,0} Tall servings"
            : $"1 pack = {ingredient.PackSize:#,0.##} {ingredient.Unit}";
    }

    private void RemoveLine(Line line)
    {
        foreach (Control control in line.Controls)
        {
            list.Controls.Remove(control);
            control.Dispose();
        }

        lines.Remove(line);

        // Close the gap
        for (int i = 0; i < lines.Count; i++)
        {
            int y = 36 + i * RowHeight;

            foreach (Control control in lines[i].Controls)
            {
                control.Top = control == lines[i].Hint
                    ? y + 30
                    : control == lines[i].Unit
                        ? y + 4
                        : control is RoundedButton
                            ? y - 2
                            : y;
            }
        }

        UpdateAddButton();
    }

    private void UpdateAddButton()
    {
        addButton.Enabled = lines.Count < ingredients.Count;
    }

    // ==============================
    // SAVE
    // ==============================

    private void Save()
    {
        List<RecipeRow> rows = new();

        foreach (Line line in lines)
        {
            if (line.Ingredient.SelectedItem is not Ingredient ingredient)
            {
                errorLabel.Text = "Choose an ingredient for every row, or remove the empty row.";
                return;
            }

            for (int i = 0; i < Recipe.Sizes.Length; i++)
            {
                rows.Add(new RecipeRow(
                    ingredient.IngredientId,
                    Recipe.Sizes[i],
                    line.Amounts[i].Value));
            }
        }

        string problem = RecipeRules.Validate(rows);

        if (problem.Length > 0)
        {
            errorLabel.Text = problem;
            return;
        }

        Rows = rows;
        DialogResult = DialogResult.OK;
        Close();
    }
}
