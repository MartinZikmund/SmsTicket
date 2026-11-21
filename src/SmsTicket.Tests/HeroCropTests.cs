using SmsTicket.Controls;
using SmsTicket.Data;

namespace SmsTicket.Tests;

[TestClass]
public class HeroCropTests
{
    private const double ViewHeight = 260;

    [TestMethod]
    [DataRow(400.0)]
    [DataRow(800.0)]
    [DataRow(1300.0)]
    [DataRow(2400.0)]
    public void Crop_AlwaysCoversTheViewport(double viewWidth)
    {
        foreach (var focusY in new[] { 0.0, 0.3, 0.5, 1.0 })
        {
            var crop = HeroCrop.Compute(1280, 853, viewWidth, ViewHeight, focusY);

            Assert.IsTrue(crop.Left <= 0.001 && crop.Left + crop.Width >= viewWidth - 0.001, $"horizontal gap at {viewWidth}/{focusY}");
            Assert.IsTrue(crop.Top <= 0.001 && crop.Top + crop.Height >= ViewHeight - 0.001, $"vertical gap at {viewWidth}/{focusY}");
        }
    }

    [TestMethod]
    public void Crop_KeepsImageAspectRatio()
    {
        var crop = HeroCrop.Compute(1280, 853, 1300, ViewHeight, 0.4);

        Assert.AreEqual(1280.0 / 853, crop.Width / crop.Height, 0.001);
    }

    [TestMethod]
    public void WideViewport_PlacesFocusInUpperPartOfViewport()
    {
        var crop = HeroCrop.Compute(1280, 960, 1300, ViewHeight, 0.4);

        var focusOnScreen = crop.Top + (0.4 * crop.Height);

        Assert.AreEqual(HeroCrop.FocusTarget * ViewHeight, focusOnScreen, 0.001);
    }

    [TestMethod]
    public void FocusNearTheTop_ClampsInsteadOfLeavingAGap()
    {
        var crop = HeroCrop.Compute(1280, 960, 1300, ViewHeight, 0.0);

        Assert.AreEqual(0, crop.Top);
    }

    [TestMethod]
    public void FocusNearTheBottom_ClampsInsteadOfLeavingAGap()
    {
        var crop = HeroCrop.Compute(1280, 960, 1300, ViewHeight, 1.0);

        Assert.AreEqual(ViewHeight, crop.Top + crop.Height, 0.001);
    }

    [TestMethod]
    public void NarrowViewport_ScalesToHeightAndCentersHorizontally()
    {
        var crop = HeroCrop.Compute(1280, 600, 400, ViewHeight, 0.5);

        Assert.AreEqual(ViewHeight, crop.Height, 0.001);
        Assert.AreEqual((400 - crop.Width) / 2, crop.Left, 0.001);
    }

    [TestMethod]
    public void UnknownImageSize_FillsViewport()
    {
        var crop = HeroCrop.Compute(0, 0, 400, ViewHeight, 0.5);

        Assert.AreEqual(new HeroCrop.Rect(0, 0, 400, ViewHeight), crop);
    }

    [TestMethod]
    public void EveryCityPhoto_HasFocusWithinRange()
    {
        foreach (var city in DataSource.Cities)
        {
            var photo = city.Photo;

            Assert.IsNotNull(photo, $"{city.Id}: missing photo credit");
            Assert.IsTrue(photo.FocusY is >= 0 and <= 1, $"{city.Id}: focus {photo.FocusY} out of range");
        }
    }

    [TestMethod]
    public void AttributionsJson_FocusMatchesPhotoCredits()
    {
        var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(FindAttributions()));

        foreach (var entry in json.RootElement.EnumerateArray())
        {
            var id = entry.GetProperty("cityId").GetString()!;
            var focus = entry.GetProperty("focusY").GetDouble();

            Assert.AreEqual(focus, PhotoCredits.ForCity(id)?.FocusY, $"{id}: focusY differs from PhotoCredits");
        }
    }

    private static string FindAttributions()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "SmsTicket", "Assets", "Cities", "attributions.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("attributions.json");
    }
}
