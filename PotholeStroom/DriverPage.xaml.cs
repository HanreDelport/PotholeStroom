using Firebase.Database;
using Firebase.Database.Query;
using PotholeStroom.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace PotholeStroom;

public partial class DriverPage : ContentPage
{
    private string _selectedStatus = "All";
    private FirebaseClient firebaseClient;
    private readonly string idToken;
    private ObservableCollection<Pothole> potholeList = new ObservableCollection<Pothole>();


    public DriverPage(string idToken)
    {
        InitializeComponent();
        this.idToken = idToken;
        firebaseClient = new FirebaseClient("https://potholestroom-default-rtdb.europe-west1.firebasedatabase.app/",
        new FirebaseOptions
        {
            AuthTokenAsyncFactory = () => Task.FromResult(idToken)
        });
        _ = LoadPotholesAsync(); // fire and forget, or await it if your constructor is async

    }

    private async void OnStatusFilterChanged(object sender, EventArgs e)
    {
        if (statusPicker.SelectedIndex == -1)
            return;

        _selectedStatus = statusPicker.SelectedItem.ToString();
        await LoadPotholesAsync(); // This should reload filtered potholes
    }

    private async void OnDonateClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.BindingContext is Pothole selectedPothole)
        {
            string result = await Application.Current.MainPage.DisplayPromptAsync(
                "Donate",
                $"Enter amount to donate toward repairing this pothole. Remaining: R{selectedPothole.RemainingCost:F2}",
                "Donate",
                "Cancel",
                keyboard: Keyboard.Numeric);

            if (decimal.TryParse(result, out decimal donationAmount))
            {
                if (donationAmount <= 0)
                {
                    await DisplayAlert("Invalid Amount", "Please enter a donation greater than 0.", "OK");
                    return;
                }

                if ((double)donationAmount > selectedPothole.RemainingCost)
                {
                    await DisplayAlert("Too Much", $"You can only donate up to {selectedPothole.RemainingCost:C}.", "OK");
                    return;
                }

                selectedPothole.AmountDonated += (double)donationAmount;


                if (selectedPothole.RemainingCost <= 0)
                {
                    selectedPothole.Status = "In Progress";
                }

                // Update Firebase
                await firebaseClient
                    .Child("potholes")
                    .Child(selectedPothole.Id)
                    .PutAsync(selectedPothole);

                await Application.Current.MainPage.DisplayAlert("Thank You", "Donation recorded successfully.", "OK");


                // Refresh list
                await LoadPotholesAsync();
            }
        }
    }

    private async Task LoadPotholesAsync()
    {
        try
        {
            var potholes = await firebaseClient
                .Child("potholes")
                .OnceAsync<Pothole>();

            var potholeList = potholes
                .Select(item =>
                {
                    var pothole = item.Object;
                    pothole.Id = item.Key;
                    return pothole;
                });

            // If a specific status is selected, filter it
            var filteredList = string.IsNullOrWhiteSpace(_selectedStatus) || _selectedStatus == "All"
                ? potholeList
                : potholeList.Where(p => p.Status?.Trim().Equals(_selectedStatus, StringComparison.OrdinalIgnoreCase) == true);

            // Then order the (possibly filtered) list
            var sortedList = filteredList
                .OrderBy(p => GetStatusOrder(p.Status))
                .ToList();

            potholesCollectionView.ItemsSource = sortedList;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not load potholes: " + ex.Message, "OK");
        }
    }



    private int GetStatusOrder(string status)
    {
        return status switch
        {
            "Unfinanced" => 0,
            "In Progress" => 1,
            "Finished" => 2,
            _ => 3
        };
    }


}
