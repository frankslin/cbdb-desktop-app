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
        var tabWritings = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabWritings");
        var tabPostings = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabPostings");
        var tabEntry = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabEntry");
        var tabEvents = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabEvents");
        var tabStatus = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabStatus");
        var tabKinship = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabKinship");
        var tabPossessions = AvaloniaUiTestHelper.FindRequiredControl<TabItem>(window, "TabPossessions");

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

        mainTabs.SelectedItem = tabWritings;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "WritingsPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Writings tab did not render any records."
        );

        mainTabs.SelectedItem = tabPostings;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "PostingsPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Postings tab did not render any records."
        );

        mainTabs.SelectedItem = tabEntry;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "EntryPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Entry tab did not render any records."
        );

        mainTabs.SelectedItem = tabEvents;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "EventsPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Events tab did not render any records."
        );

        mainTabs.SelectedItem = tabStatus;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "StatusPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Status tab did not render any records."
        );

        mainTabs.SelectedItem = tabKinship;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "KinshipPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Kinship tab did not render any records."
        );

        mainTabs.SelectedItem = tabPossessions;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "PossessionsPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Possessions tab did not render any records."
        );

        var summaryPath = AvaloniaUiTestHelper.WriteJsonArtifact(
            testName,
            "summary.json",
            new {
                PersonName = valName.Text,
                Gender = valGender.Text,
                BirthYear = valBirthYear.Text,
                AddressCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AddressesPanel").Children.Count,
                AltNameCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "AltNamesPanel").Children.Count,
                WritingCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "WritingsPanel").Children.Count,
                PostingCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "PostingsPanel").Children.Count,
                EntryCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "EntryPanel").Children.Count,
                EventCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "EventsPanel").Children.Count,
                StatusCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "StatusPanel").Children.Count,
                KinshipCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "KinshipPanel").Children.Count,
                PossessionCards = AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "PossessionsPanel").Children.Count
            }
        );

        mainTabs.SelectedItem = tabWritings;
        await AvaloniaUiTestHelper.WaitUntilAsync(
            () => AvaloniaUiTestHelper.FindRequiredControl<StackPanel>(window, "WritingsPanel").Children.Count > 0,
            TimeSpan.FromSeconds(5),
            "Writings tab was not available for screenshot capture."
        );

        var writingsScreenshotPath = AvaloniaUiTestHelper.WriteArtifact(
            testName,
            "writings-tab.png",
            path => {
                var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("Headless renderer did not return a frame.");
                frame.Save(path);
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
        Assert.True(File.Exists(writingsScreenshotPath), "Writings tab screenshot artifact was not written.");

        window.Close();
        File.Delete(sqlitePath);
    }
}
