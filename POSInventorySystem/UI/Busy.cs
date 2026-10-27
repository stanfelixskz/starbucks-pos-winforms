namespace POSInventorySystem.UI;

// Runs a database save on a background thread so the window keeps painting.
// While it runs, a small "Saving..." box blocks the window (so nothing else
// can be clicked and change the data being saved) and the cursor shows
// as busy. Quick saves finish before the box would even appear.
public static class Busy
{
    // A save faster than this never shows the box
    private const int ShowAfterMs = 150;

    // An exception thrown by the work is thrown again here, unchanged,
    // so the caller's catch block works as it did before.
    public static T Run<T>(IWin32Window? owner, Func<T> work, string text = "Saving...")
    {
        Task<T> task = Task.Run(work);

        // WaitAny does not throw when the work failed; the failure is
        // rethrown below as the original exception
        if (Task.WaitAny(new Task[] { task }, ShowAfterMs) < 0)
        {
            Cursor previous = Cursor.Current ?? Cursors.Default;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                using SavingBox box = new SavingBox(text, task);
                box.ShowDialog(owner);
            }
            finally
            {
                Cursor.Current = previous;
            }
        }

        return task.GetAwaiter().GetResult();
    }

    public static void Run(IWin32Window? owner, Action work, string text = "Saving...") =>
        Run<bool>(owner, () =>
        {
            work();
            return true;
        }, text);

    private sealed class SavingBox : Form
    {
        public SavingBox(string text, Task task)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            ControlBox = false;
            Size = new Size(260, 80);
            BackColor = Theme.GreenDark;
            UseWaitCursor = true;

            Label label = Theme.Label(text, 13f, true, Color.White);
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(label);

            Shown += async (_, _) =>
            {
                try
                {
                    await task;
                }
                catch (Exception)
                {
                    // the caller sees the exception; this box only closes
                }

                Close();
            };
        }

        // Alt+F4 must not close the box while the save is running
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = e.CloseReason == CloseReason.UserClosing;
            base.OnFormClosing(e);
        }
    }
}
