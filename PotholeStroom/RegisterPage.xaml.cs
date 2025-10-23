using PotholeStroom.Services;

namespace PotholeStroom;

public partial class RegisterPage : ContentPage
{
    private readonly FirebaseAuthService _authService;

    public RegisterPage()
    {
        InitializeComponent();
        _authService = new FirebaseAuthService();
    }

    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        // Ask user to pick role
        string action = await DisplayActionSheet("Select User Role", "Cancel", null, "Driver", "Road Worker");

        if (action == "Cancel" || string.IsNullOrEmpty(action))
            return;

        string email = emailEntry.Text;
        string password = passwordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please enter both email and password.", "OK");
            return;
        }

        // Call your register method with the selected role
        var result = await _authService.RegisterUser(email, password, action);

        await DisplayAlert("Info", result, "OK");
    }




    private async void OnSignInClicked(object sender, EventArgs e)
    {
        var email = emailEntry.Text?.Trim();
        var password = passwordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            messageLabel.Text = "Email and password are required.";
            return;
        }

        var result = await _authService.LoginUser(email, password);
        messageLabel.Text = result;
    }
}
