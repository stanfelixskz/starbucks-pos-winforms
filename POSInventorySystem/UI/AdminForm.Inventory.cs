using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem.UI;

// One Inventory page with three tabs:
//   Ingredients | Products | Stock Records
// Search works on every tab; the low-stock filter applies to the first
// two; the activity filter applies to the records.
public partial class AdminForm
{
    private const string ActionColumn = "Action";

    private const int TabIngredients = 0;
    private const int TabProducts = 1;
    private const int TabRecords = 2;

    private readonly InventoryService inventoryService = new InventoryService();

    private int inventoryTab = TabIngredients;
    private bool lowOnly;

    private Panel inventoryTabs = null!;
    private Panel lowFilter = null!;
    private ComboBox recordsFilter = null!;
    private ComboBox recordsRange = null!;
    private Panel inventoryToolbar = null!;
    private TextBox inventorySearch = null!;
    private Label inventorySummary = null!;
    private Label inventoryLow = null!;
    private Label inventoryStatus = null!;
    private RoundedButton addProductButton = null!;
    private RoundedButton editProductButton = null!;
    private RoundedButton receiveButton = null!;
    private RoundedButton orderListButton = null!;
    private RoundedButton recipesButton = null!;
    private RoundedButton availabilityButton = null!;
    private RoundedButton wasteButton = null!;
    private RoundedButton adjustButton = null!;

    private Panel ingredientCard = null!;
    private Panel productCard = null!;
    private Panel recordsCard = null!;

    // ==============================
    // BUILD
    // ==============================

    private Control BuildInventoryPage()
    {
        Panel root = new Panel();

        // ---- Toolbar ----
        Panel toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 130
        };

        inventoryToolbar = toolbar;

        inventoryTabs = MakeToggle(
            new[] { "Ingredients", "Products", "Stock Records" },
            TabIngredients,
            index =>
            {
                inventoryTab = index;
                RefreshInventory();
            },
            buttonWidth: 118);

        inventoryTabs.Location = new Point(0, 0);

        lowFilter = MakeToggle(
            new[] { "All", "Low stock" },
            0,
            index =>
            {
                lowOnly = index == 1;
                RefreshInventory();
            },
            buttonWidth: 92);

        lowFilter.Location = new Point(390, 0);

        recordsFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 150,
            Font = Theme.Regular(10.5f),
            Location = new Point(390, 4)
        };

        recordsFilter.Items.AddRange(
            new object[] { "All activity", "Receives", "Sales", "Waste & adjust" });
        recordsFilter.SelectedIndex = 0;
        recordsFilter.SelectedIndexChanged += (_, _) => RefreshInventory();

        recordsRange = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140,
            Font = Theme.Regular(10.5f),
            Location = new Point(552, 4)
        };

        recordsRange.Items.AddRange(
            new object[] { "Any date", "Today", "Last 7 days", "Last 30 days" });
        recordsRange.SelectedIndex = 0;
        recordsRange.SelectedIndexChanged += (_, _) => RefreshInventory();

        inventorySearch = Theme.TextBox(230);
        inventorySearch.Location = new Point(600, 4);
        inventorySearch.PlaceholderText = "Search name, ID or order";
        inventorySearch.TextChanged += (_, _) => RefreshInventory();

        addProductButton = Theme.PrimaryButton("+  Add product");
        addProductButton.Size = new Size(160, 36);
        addProductButton.Font = Theme.Bold(9.5f);
        addProductButton.Click += (_, _) =>
        {
            if (inventoryTab == TabIngredients)
            {
                AddIngredientClicked();
            }
            else
            {
                AddProductClicked();
            }
        };

        editProductButton = Theme.SecondaryButton("Edit selected");
        editProductButton.Size = new Size(130, 36);
        editProductButton.Font = Theme.Bold(9.5f);
        editProductButton.Click += (_, _) =>
        {
            if (inventoryTab == TabIngredients)
            {
                EditIngredientClicked();
            }
            else
            {
                EditProductClicked();
            }
        };

        orderListButton = Theme.SecondaryButton("Order list");
        orderListButton.Size = new Size(110, 36);
        orderListButton.Font = Theme.Bold(9.5f);
        orderListButton.Click += (_, _) =>
        {
            using OrderListDialog dialog = new OrderListDialog();
            dialog.ShowDialog(this);
        };

        availabilityButton = Theme.SecondaryButton("Stop selling");
        availabilityButton.Size = new Size(130, 36);
        availabilityButton.Font = Theme.Bold(9.5f);
        availabilityButton.Click += (_, _) => ToggleAvailabilityClicked();

        recipesButton = Theme.SecondaryButton("Recipes");
        recipesButton.Size = new Size(110, 36);
        recipesButton.Font = Theme.Bold(9.5f);
        recipesButton.Click += (_, _) =>
        {
            using RecipesDialog dialog = new RecipesDialog();
            dialog.ShowDialog(this);
        };

        receiveButton = Theme.PrimaryButton("Receive");
        receiveButton.Size = new Size(120, 36);
        receiveButton.Font = Theme.Bold(9.5f);
        receiveButton.Location = new Point(0, 46);
        receiveButton.Click += (_, _) => ActOnSelected(StockAction.Receive);

        wasteButton = Theme.DangerButton("Waste");
        wasteButton.Size = new Size(100, 36);
        wasteButton.Font = Theme.Bold(9.5f);
        wasteButton.Location = new Point(132, 46);
        wasteButton.Click += (_, _) => ActOnSelected(StockAction.Waste);

        adjustButton = Theme.SecondaryButton("Adjust count");
        adjustButton.Size = new Size(128, 36);
        adjustButton.Font = Theme.Bold(9.5f);
        adjustButton.Location = new Point(244, 46);
        adjustButton.Click += (_, _) => ActOnSelected(StockAction.Adjust);

        // Add / Edit sit at the right edge of the second row
        toolbar.Resize += (_, _) =>
        {
            editProductButton.Location =
                new Point(toolbar.Width - editProductButton.Width, 46);

            addProductButton.Location =
                new Point(editProductButton.Left - addProductButton.Width - 10, 46);

            orderListButton.Location =
                new Point(addProductButton.Left - orderListButton.Width - 10, 46);

            recipesButton.Location =
                new Point(addProductButton.Left - recipesButton.Width - 10, 46);

            availabilityButton.Location =
                new Point(recipesButton.Left - availabilityButton.Width - 10, 46);

            orderListButton.Location = recipesButton.Location;
        };

        inventorySummary = Theme.Label("", 10f, false, Theme.Muted);
        inventorySummary.Location = new Point(2, 94);

        inventoryLow = Theme.Label("", 10f, true, Theme.Danger);
        inventoryLow.Location = new Point(2, 94);

        inventoryStatus = Theme.Label("", 10f, true, Theme.Green);
        inventoryStatus.Location = new Point(2, 112);

        toolbar.Controls.AddRange(new Control[]
        {
            inventoryTabs, lowFilter, recordsFilter, recordsRange,
            inventorySearch, receiveButton, wasteButton, adjustButton,
            orderListButton, recipesButton, availabilityButton,
            addProductButton, editProductButton,
            inventorySummary, inventoryLow, inventoryStatus
        });

        // ---- Tables (one per tab) ----
        ingredientGrid = MakeGrid(
            new[] { "ID", "Ingredient", "In stock", "Reorder at", "Par", "Status", ActionColumn },
            55, 150, 250, 80, 55, 65, 80);

        productGrid = MakeGrid(
            new[] { "ID", "Name", "Type", "Price", "Stock", "Low at", "Status", ActionColumn },
            55, 170, 80, 85, 130, 70, 65, 80);

        stockGrid = MakeGrid(
            new[] { "Date / time", "Type", "Item", "Quantity", "Change", "Reference", "Details" },
            175, 70, 160, 100, 70, 65, 215);

        foreach (DataGridView grid in new[] { ingredientGrid, productGrid })
        {
            StyleActionColumn(grid);
            grid.CellClick += InventoryCellClick;
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    OpenAction(StockAction.Receive, grid.Rows[e.RowIndex].Tag as string);
                }
            };
        }

        ingredientCard = TitledCard(
            "Ingredients  ·  counted in packs (1 pack = the amount shown)", ingredientGrid);
        productCard = TitledCard(
            "Products  ·  food counted in pieces, drinks show if they can be sold", productGrid);
        recordsCard = TitledCard(
            "Stock records  ·  dated restocks and sale deductions", stockGrid);

        root.Controls.Add(ingredientCard);
        root.Controls.Add(productCard);
        root.Controls.Add(recordsCard);
        root.Controls.Add(toolbar);

        return root;
    }

    // The Action column shows a green "Restock" link on rows that can be
    // restocked.
    private static void StyleActionColumn(DataGridView grid)
    {
        DataGridViewColumn column = grid.Columns[ActionColumn]!;

        column.HeaderText = "";
        column.DefaultCellStyle.ForeColor = Theme.Green;
        column.DefaultCellStyle.Font = Theme.Bold(10f);
        column.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleRight;

        grid.CellMouseMove += (_, e) =>
        {
            bool onAction = e.RowIndex >= 0 &&
                grid.Columns[e.ColumnIndex].Name == ActionColumn &&
                (grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "").Length > 0;

            grid.Cursor = onAction ? Cursors.Hand : Cursors.Default;
        };

        grid.MouseLeave += (_, _) => grid.Cursor = Cursors.Default;
    }

    // ==============================
    // REFRESH
    // ==============================

    private static string IngredientStatus(Ingredient i) =>
        i.Quantity <= 0 ? "OUT" : i.IsLowStock() ? "LOW" : "OK";

    // Food: by pieces. Drinks have no stock of their own, so they only show
    // whether they can still be sold: OK, OUT (an ingredient is missing)
    // or OFF (stopped by hand).
    private string ProductStatus(Product p)
    {
        if (p is BeverageProduct)
        {
            if (p.Unavailable)
            {
                return "OFF";
            }

            return inventoryService.MissingIngredients(p).Count > 0
                ? "OUT"
                : "OK";
        }

        if (p.Unavailable)
        {
            return "OFF";
        }

        return p.Stock <= 0 ? "OUT" : p.IsLowStock() ? "LOW" : "OK";
    }

    // What the Stock column says for a product
    private string ProductStockText(Product p)
    {
        if (p is not BeverageProduct)
        {
            return $"{p.Stock} pcs";
        }

        if (p.Unavailable)
        {
            return "Stopped by hand";
        }

        List<Ingredient> missing = inventoryService.MissingIngredients(p);

        if (missing.Count == 0)
        {
            return "Available";
        }

        string names = string.Join(", ", missing.Take(2).Select(i => i.Name));

        return missing.Count > 2
            ? $"Needs {names} +{missing.Count - 2}"
            : $"Needs {names}";
    }

    private static int LowStockCount() =>
        DataStore.Ingredients.Count(i => i.IsLowStock()) +
        DataStore.Products.Count(p => p is FoodProduct && p.IsLowStock());

    private void RefreshInventory()
    {
        string search = inventorySearch.Text.Trim();

        bool Matches(params string[] fields) =>
            search.Length == 0 ||
            fields.Any(f => f.Contains(search, StringComparison.OrdinalIgnoreCase));

        // ---- Ingredients ----
        ingredientGrid.Rows.Clear();

        foreach (Ingredient i in DataStore.Ingredients)
        {
            string status = IngredientStatus(i);

            if ((lowOnly && status == "OK") ||
                !Matches(i.IngredientId, i.Name))
            {
                continue;
            }

            int row = ingredientGrid.Rows.Add(
                i.IngredientId,
                i.Name,
                Ingredient.Packs(i.Quantity),
                $"{i.LowStockThreshold:0}",
                $"{i.ParLevel:0}",
                status,
                "Receive");

            Tint(ingredientGrid.Rows[row], status);
            ingredientGrid.Rows[row].Tag = $"Ingredient:{i.IngredientId}";
        }

        // ---- Products ----
        productGrid.Rows.Clear();

        foreach (Product p in DataStore.Products)
        {
            string status = ProductStatus(p);

            if ((lowOnly && status != "LOW" && status != "OUT") ||
                !Matches(p.ProductId, p.Name))
            {
                continue;
            }

            bool beverage = p is BeverageProduct;

            int row = productGrid.Rows.Add(
                p.ProductId,
                p.Name,
                p.Category,
                $"₱{p.Price:N2}",
                ProductStockText(p),
                beverage ? "-" : $"{p.LowStockLevel} pcs",
                status,
                beverage && status != "OUT" ? "" : "Receive");

            Tint(productGrid.Rows[row], status);
            productGrid.Rows[row].Tag = $"Product:{p.ProductId}";
        }

        // ---- Stock records ----
        stockGrid.Rows.Clear();

        IEnumerable<StockRecord> records = DataStore.StockRecords;

        if (recordsFilter.SelectedIndex == 1)
        {
            records = records.Where(r => r.Type == "RESTOCK");
        }
        else if (recordsFilter.SelectedIndex == 2)
        {
            records = records.Where(r => r.Type == "SALE");
        }
        else if (recordsFilter.SelectedIndex == 3)
        {
            records = records.Where(r => r.Type == "WASTE" || r.Type == "ADJUST");
        }

        DateTime? since = recordsRange.SelectedIndex switch
        {
            1 => DateTime.Today,
            2 => DateTime.Today.AddDays(-6),
            3 => DateTime.Today.AddDays(-29),
            _ => null
        };

        if (since.HasValue)
        {
            records = records.Where(r => r.Timestamp >= since.Value);
        }

        foreach (StockRecord r in records
                     .Where(r => Matches(r.ItemId, r.ItemName, r.Reference, r.Note))
                     .OrderByDescending(r => r.Timestamp))
        {
            string unit = r.Unit == "pack" && r.Type == "SALE"
                ? "packs"
                : r.Unit;

            stockGrid.Rows.Add(
                r.Timestamp.ToString("MMM dd, hh:mm:ss tt"),
                TypeLabel(r.Type),
                $"{r.ItemId}  {r.ItemName}",
                $"{r.Count:0.##} {unit}",
                r.StockChange > 0
                    ? $"+{r.StockChange:0.##}"
                    : $"{r.StockChange:0.##}",
                r.Reference,
                r.Note);
        }

        // ---- Show only the chosen tab and its controls ----
        ingredientCard.Visible = inventoryTab == TabIngredients;
        productCard.Visible = inventoryTab == TabProducts;
        recordsCard.Visible = inventoryTab == TabRecords;

        lowFilter.Visible = inventoryTab != TabRecords;
        recordsFilter.Visible = inventoryTab == TabRecords;
        recordsRange.Visible = inventoryTab == TabRecords;

        bool onItems = inventoryTab != TabRecords;

        receiveButton.Visible = onItems;
        wasteButton.Visible = onItems;
        adjustButton.Visible = onItems;

        addProductButton.Visible = onItems;
        editProductButton.Visible = onItems;
        orderListButton.Visible = inventoryTab == TabIngredients;
        recipesButton.Visible = inventoryTab == TabProducts;
        availabilityButton.Visible = inventoryTab == TabProducts;
        UpdateAvailabilityButton();

        addProductButton.Text = inventoryTab == TabIngredients
            ? "+  Add ingredient"
            : "+  Add product";

        // The records tab has no button row, so close the gap
        inventorySearch.Left = onItems ? 610 : 712;
        inventoryToolbar.Height = onItems ? 130 : 96;

        inventorySummary.Top = onItems ? 94 : 56;
        inventoryLow.Top = inventorySummary.Top;
        inventoryStatus.Top = inventorySummary.Top + 18;
        inventoryToolbar.PerformLayout();

        // ---- Summary line ----
        int low = LowStockCount();

        inventorySummary.Text = inventoryTab == TabRecords
            ? $"{stockGrid.Rows.Count} of {DataStore.StockRecords.Count} records"
            : $"{DataStore.Ingredients.Count} ingredients  ·  " +
              $"{DataStore.Products.Count} products";

        inventoryLow.Text = low > 0 ? $"   ·   {low} low on stock" : "";
        inventoryLow.Left = inventorySummary.Right - 4;

        UpdateInventoryBadge();
    }

    private static void Tint(DataGridViewRow row, string status)
    {
        if (status == "LOW" || status == "OUT")
        {
            row.DefaultCellStyle.BackColor =
                ColorTranslator.FromHtml("#FDEDEB");
        }
    }

    // Shows the low-stock count next to Inventory in the sidebar
    private void UpdateInventoryBadge()
    {
        if (!navButtons.TryGetValue("inventory", out RoundedButton? nav))
        {
            return;
        }

        int low = LowStockCount();

        nav.BadgeText = low > 0 ? low.ToString() : "";
        nav.Invalidate();
    }

    // Switches the Inventory page to a tab (used by the dashboard)
    private void ShowInventoryTab(int tab)
    {
        ShowPage("inventory");
        ((RoundedButton)inventoryTabs.Controls[tab]).PerformClick();
    }

    // ==============================
    // STOCK ACTIONS (Receive / Waste / Adjust)
    // ==============================

    // Human label for the type column
    private static string TypeLabel(string type) => type switch
    {
        "RESTOCK" => "Receive",
        "SALE" => "Sale",
        "WASTE" => "Waste",
        "ADJUST" => "Adjust",
        _ => type
    };

    // The toolbar buttons act on the row selected on the current tab
    private void ActOnSelected(StockAction action)
    {
        DataGridView grid = inventoryTab == TabIngredients
            ? ingredientGrid
            : productGrid;

        string? tag = grid.CurrentRow?.Tag as string;

        if (tag == null)
        {
            MessageBox.Show(
                "Select an item in the list first.",
                "Stock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        OpenAction(action, tag);
    }

    private void InventoryCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (sender is DataGridView grid &&
            e.RowIndex >= 0 &&
            grid.Columns[e.ColumnIndex].Name == ActionColumn &&
            (grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "").Length > 0)
        {
            OpenAction(StockAction.Receive, grid.Rows[e.RowIndex].Tag as string);
        }
    }

    // typeAndId is "Ingredient:I001" or "Product:P07"
    private void OpenAction(StockAction action, string? typeAndId)
    {
        if (typeAndId == null)
        {
            return;
        }

        string[] parts = typeAndId.Split(':');

        if (parts[0] == "Ingredient")
        {
            Ingredient? ingredient = DataStore.GetIngredient(parts[1]);

            if (ingredient == null)
            {
                return;
            }

            using StockActionDialog dialog = new StockActionDialog(
                action,
                ingredient.IngredientId,
                ingredient.Name,
                ingredient.Quantity,
                isIngredient: true,
                stockText: Ingredient.Packs(ingredient.Quantity));

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                switch (action)
                {
                    case StockAction.Receive:
                        decimal added = adminService.RestockIngredient(
                            ingredient, dialog.Unit, dialog.Count,
                            dialog.PacksPerBox, dialog.Note);

                        inventoryStatus.Text =
                            $"Received {ingredient.Name}: +{added:0} packs " +
                            $"({DateTime.Now:hh:mm:ss tt})";
                        break;

                    case StockAction.Waste:
                        adminService.WasteIngredient(
                            ingredient, dialog.Unit, dialog.Count,
                            dialog.PacksPerBox, dialog.Reason, dialog.Note);

                        inventoryStatus.Text =
                            $"Waste recorded for {ingredient.Name} ({DateTime.Now:hh:mm:ss tt})";
                        break;

                    default:
                        adminService.AdjustIngredient(
                            ingredient, dialog.Count, dialog.Reason, dialog.Note);

                        inventoryStatus.Text =
                            $"Count saved for {ingredient.Name} ({DateTime.Now:hh:mm:ss tt})";
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowSaveWarning(ex);
            }
        }
        else
        {
            Product? product = DataStore.GetProduct(parts[1]);

            if (product == null)
            {
                return;
            }

            if (product is BeverageProduct)
            {
                if (action == StockAction.Receive)
                {
                    RestockDrink(product);
                }
                else
                {
                    MessageBox.Show(
                        "Drinks are made from ingredients, so they have no " +
                        "stock of their own. Count or waste the ingredients instead.",
                        "Stock",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return;
            }

            using StockActionDialog dialog = new StockActionDialog(
                action,
                product.ProductId,
                product.Name,
                product.Stock,
                isIngredient: false);

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                switch (action)
                {
                    case StockAction.Receive:
                        adminService.RestockProduct(product, dialog.Count, dialog.Note);

                        inventoryStatus.Text =
                            $"Received {product.Name}: +{dialog.Count} pcs " +
                            $"({DateTime.Now:hh:mm:ss tt})";
                        break;

                    case StockAction.Waste:
                        adminService.WasteProduct(
                            product, dialog.Count, dialog.Reason, dialog.Note);

                        inventoryStatus.Text =
                            $"Waste recorded for {product.Name} ({DateTime.Now:hh:mm:ss tt})";
                        break;

                    default:
                        adminService.AdjustProduct(
                            product, dialog.Count, dialog.Reason, dialog.Note);

                        inventoryStatus.Text =
                            $"Count saved for {product.Name} ({DateTime.Now:hh:mm:ss tt})";
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowSaveWarning(ex);
            }
        }

        RefreshInventory();
    }

    // The button says what a click will do for the selected product
    private void UpdateAvailabilityButton()
    {
        string? tag = productGrid.CurrentRow?.Tag as string;
        Product? product = tag == null
            ? null
            : DataStore.GetProduct(tag.Split(':')[1]);

        availabilityButton.Text = product?.Unavailable == true
            ? "Resume selling"
            : "Stop selling";
    }

    // Switch the selected item off (or back on) for sale, like a
    // cafe marking an item "86".
    private void ToggleAvailabilityClicked()
    {
        string? tag = productGrid.CurrentRow?.Tag as string;
        Product? product = tag == null
            ? null
            : DataStore.GetProduct(tag.Split(':')[1]);

        if (product == null)
        {
            MessageBox.Show("Select a product first.", "Availability",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!adminService.SetUnavailable(product, !product.Unavailable, out string error))
        {
            MessageBox.Show(error, "Availability",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        inventoryStatus.Text = product.Unavailable
            ? $"{product.Name} is no longer being sold ({DateTime.Now:hh:mm:ss tt})"
            : $"{product.Name} is being sold again ({DateTime.Now:hh:mm:ss tt})";

        RefreshInventory();
    }

    // A drink is out because an ingredient is out. Offer exactly those
    // ingredients so they can be restocked in one step.
    private void RestockDrink(Product drink)
    {
        Recipe? recipe = RecipeData.Recipes.FirstOrDefault(
            r => r.Product.ProductId == drink.ProductId);

        if (recipe == null)
        {
            return;
        }

        // The ingredient(s) that run out first, plus anything already low
        decimal fewest = recipe.Ingredients
            .Where(l => l.QuantityRequired > 0)
            .Min(l => l.Ingredient.Quantity / l.QuantityRequired);

        List<Ingredient> holdingBack = recipe.Ingredients
            .Where(l => l.QuantityRequired > 0 &&
                (l.Ingredient.IsLowStock() ||
                 l.Ingredient.Quantity / l.QuantityRequired <= fewest + 0.0001m))
            .Select(l => l.Ingredient)
            .DistinctBy(i => i.IngredientId)
            .ToList();

        using DrinkRestockDialog dialog = new DrinkRestockDialog(
            drink.Name,
            holdingBack);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            int count = 0;

            foreach (var (ingredient, packs) in dialog.Received)
            {
                adminService.RestockIngredient(
                    ingredient, "pack", packs, 1, $"For {drink.Name}");

                count++;
            }

            if (count > 0)
            {
                inventoryStatus.Text =
                    $"Restocked {count} ingredient(s) for {drink.Name} " +
                    $"({DateTime.Now:hh:mm:ss tt})";
            }
        }
        catch (Exception ex)
        {
            ShowSaveWarning(ex);
        }

        RefreshInventory();
    }

    private static void ShowSaveWarning(Exception ex)
    {
        MessageBox.Show(
            "The restock was applied but could not be saved to the " +
            "database yet:" + Environment.NewLine + ex.Message,
            "Database",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    // Used by the dashboard: open Inventory on the right tab with the
    // restock popup ready for that item.
    private void GoRestock(string? typeAndId)
    {
        inventorySearch.Clear();
        ((RoundedButton)lowFilter.Controls[0]).PerformClick();

        ShowInventoryTab(
            typeAndId?.StartsWith("Product") == true
                ? TabProducts
                : TabIngredients);

        OpenAction(StockAction.Receive, typeAndId);
    }

    // ==============================
    // ADD / EDIT PRODUCT
    // ==============================

    private void AddIngredientClicked()
    {
        using IngredientDialog dialog =
            new IngredientDialog(adminService.NextIngredientId(), null);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!adminService.AddIngredient(
            dialog.ItemName,
            dialog.Unit,
            dialog.PackSize,
            dialog.LowAt,
            dialog.Par,
            dialog.StartingPacks,
            out string error))
        {
            MessageBox.Show(error, "Add ingredient",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RefreshInventory();
    }

    private void EditIngredientClicked()
    {
        string? tag = ingredientGrid.CurrentRow?.Tag as string;

        Ingredient? ingredient = tag == null
            ? null
            : DataStore.GetIngredient(tag.Split(':')[1]);

        if (ingredient == null)
        {
            MessageBox.Show("Select an ingredient first.", "Edit ingredient",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using IngredientDialog dialog =
            new IngredientDialog(ingredient.IngredientId, ingredient);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!adminService.UpdateIngredient(
            ingredient,
            dialog.ItemName,
            dialog.Unit,
            dialog.PackSize,
            dialog.LowAt,
            dialog.Par,
            out string error))
        {
            MessageBox.Show(error, "Edit ingredient",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RefreshInventory();
    }

    private void AddProductClicked()
    {
        using ProductDialog dialog =
            new ProductDialog(adminService.NextProductId(), null);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!adminService.AddProduct(
            dialog.ItemName,
            dialog.Price,
            dialog.Stock,
            dialog.IsBeverage,
            dialog.LowStockLevel,
            out string error))
        {
            MessageBox.Show(error, "Add product",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RefreshInventory();
    }

    private void EditProductClicked()
    {
        string? tag = productGrid.CurrentRow?.Tag as string;

        Product? product = tag == null
            ? null
            : DataStore.GetProduct(tag.Split(':')[1]);

        if (product == null)
        {
            MessageBox.Show("Select a product first.", "Edit product",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using ProductDialog dialog =
            new ProductDialog(product.ProductId, product);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!adminService.UpdateProduct(
            product,
            dialog.ItemName,
            dialog.Price,
            dialog.LowStockLevel,
            out string error))
        {
            MessageBox.Show(error, "Edit product",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RefreshInventory();
    }
}
