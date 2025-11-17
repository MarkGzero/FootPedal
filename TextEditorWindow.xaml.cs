using System;
using System.Windows;

namespace FootPedalApp
{
    public partial class TextEditorWindow : Window
    {
        public string EditedText { get; private set; }

        public TextEditorWindow(string initialText, string title = "Edit Command")
        {
            InitializeComponent();
            txtEditor.Text = initialText;
            Title = title;
            EditedText = initialText;

            // Select all text when window opens
            txtEditor.Focus();
            txtEditor.SelectAll();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            EditedText = txtEditor.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
