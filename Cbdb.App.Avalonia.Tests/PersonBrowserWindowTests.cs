using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Cbdb.App.Avalonia.Browser;
using Cbdb.App.Avalonia.Localization;
using Cbdb.App.Avalonia.Tests.TestDoubles;
using Cbdb.App.Avalonia.Tests.TestInfrastructure;
using Cbdb.App.Core;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class PersonBrowserWindowTests {
    [AvaloniaFact]
    public async Task PersonBrowserWindow_LoadsFixtureData_AndRendersLazyTabs() {
        const string testName = nameof(PersonBrowserWindow_LoadsFixtureData_AndRendersLazyTabs);

        var localization = new AppLocalizationService();
        localization.SetLanguage(UiLanguage.English);

        var sqlitePath = Path.Combine(Path.GetTempPath(), "headless-fixture.sqlite3");
        File.WriteAllText(sqlitePath, "fixture");

        var window = new PersonBrowserWindow(sqlitePath, localization, new FakePersonBrowserService());
        window.Show();

        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<DataGrid>(window, "GridPeople").SelectedItem is not null,
            TimeSpan.FromSeconds(5),
            "Person list did not finish its initial load."
        );

        var valName = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValName");
        var valGender = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValGender");
        var valBirthYear = AvaloniaUiTestHelper.FindRequiredControl<TextBox>(window, "ValBasic_birth_year");

        Assert.Equal("Su Shi", valName.Text);
        Assert.Equal("Male", valGender.Text);
        Assert.Equal("1037", valBirthYear.Text);

        var mainTabs = AvaloniaUiTestHelper.FindRequiredControl<TabControl>(window, "MainTabs");
        var tabAddresses = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabAddresses");
        var tabAltNames = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabAltNames");

        mainTabs.SelectedItem = tabAddresses;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AddressesPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Addresses tab did not render any address cards."
        );

        mainTabs.SelectedItem = tabAltNames;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AltNamesPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Alt names tab did not render any records."
        );

        var summaryPath = AvaloniaUiTestHelper.WriteJsonArtifact(
            testName,
            "summary.json",
            new {
                PersonName = valName.Text,
                Gender = valGender.Text,
                BirthYear = valBirthYear.Text,
                AddressCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AddressesPanel").Children.Count,
                AltNameCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AltNamesPanel").Children.Count
            }
        );

        var screenshotPath = AvaloniaUiTestHelper.WriteArtifact(
            testName,
            "person-browser.png",
            path => {
                var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("Headless renderer did not return a frame.");
                frame.Save(path);
            }
        );

        Assert.True(File.Exists(summaryPath), "Structured UI summary artifact was not written.");
        Assert.True(File.Exists(screenshotPath), "Rendered screenshot artifact was not written.");

        window.Close();
        File.Delete(sqlitePath);
    }
}
