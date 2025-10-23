using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Controls;
using PotholeStroom.Models;
using System.Collections.ObjectModel;

namespace PotholeStroom;

public partial class RoadWorkerPage : ContentPage
{
    private FirebaseClient _firebaseClient;
    private ObservableCollection<Pothole> _potholes;
    private string _userId;
    private string _idToken;

    public RoadWorkerPage(string userId, string idToken)
    {
        InitializeComponent();
        Device.BeginInvokeOnMainThread(() =>
        {
            statusPicker.SelectedIndex = 0;
        });
        _userId = userId;
        _idToken = idToken;
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Logged-in workerId: {_userId}");


        _firebaseClient = new FirebaseClient(
            "https://potholestroom-default-rtdb.europe-west1.firebasedatabase.app/",
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(_idToken)
            });

        _potholes = new ObservableCollection<Pothole>();
        potholesCollectionView.ItemsSource = _potholes;
        LoadUserPotholes("All");
    }

    private async void OnStatusFilterChanged(object sender, EventArgs e)
    {
        if (statusPicker.SelectedIndex == -1)
            return;

        string selectedStatus = statusPicker.SelectedItem.ToString();

        await LoadUserPotholes(selectedStatus);
    }

    private async Task LoadUserPotholes(string statusFilter = "All")
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] LoadUserPotholes() called with filter: " + statusFilter);

        var items = await _firebaseClient
            .Child("potholes")
            .OrderBy("WorkerId")
            .EqualTo(_userId)
            .OnceAsync<Pothole>();

        _potholes.Clear();

        var filtered = items
            .Select(item =>
            {
                var pothole = item.Object;
                pothole.Id = item.Key;
                return pothole;
            });

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            filtered = filtered.Where(p => p.Status?.Trim().Equals(statusFilter, StringComparison.OrdinalIgnoreCase) == true);
        }

        var sorted = filtered.OrderBy(p => GetStatusOrder(p.Status)).ToList();

        foreach (var pothole in sorted)
        {
            _potholes.Add(pothole);
        }
    }

    private int GetStatusOrder(string status)
    {
        return status switch
        {
            "In Progress" => 0,
            "Unfinanced" => 1,
            "Finished" => 2,
            _ => 3
        };
    }

    private async void OnFinishedClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Pothole pothole)
        {
            // Update status
            pothole.Status = "Finished";

            try
            {
                await _firebaseClient
                    .Child("potholes")
                    .Child(pothole.Id)
                    .PutAsync(pothole);

                await DisplayAlert("Success", "Pothole marked as finished.", "OK");

                // Refresh list with current filter
                await LoadUserPotholes(statusPicker.SelectedItem?.ToString() ?? "All");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to update pothole: {ex.Message}", "OK");
            }
        }
    }

    private async void OnSubmitPotholeClicked(object sender, EventArgs e)
    {
        string location = locationEntry.Text;
        bool isCostValid = double.TryParse(costEntry.Text, out double cost);

        if (string.IsNullOrWhiteSpace(location) || !isCostValid || cost <= 0)
        {
            messageLabel.Text = "Please enter valid location and cost.";
            messageLabel.TextColor = Colors.Red;
            return;
        }

        var pothole = new Pothole
        {
            WorkerId = _userId,
            Location = location,
            Cost = cost,
            AmountDonated = 0,
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        await _firebaseClient
            .Child("potholes")
            .PostAsync(pothole);

        messageLabel.Text = "Pothole added!";
        messageLabel.TextColor = Colors.Green;

        locationEntry.Text = string.Empty;
        costEntry.Text = string.Empty;

        LoadUserPotholes(); // Refresh list
    }
}
