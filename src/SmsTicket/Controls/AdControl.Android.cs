#if __ANDROID__
using Android.Widget;
using Google.Android.Gms.Ads;
using static Android.Views.ViewGroup;

namespace SmsTicket.Controls;

partial class AdControl
{
    public AdControl()
    {
        var adUnitId = App.AppConfig.AndroidAdUnitId;
        if (string.IsNullOrEmpty(adUnitId))
            return;

        var adView = new AdView(ContextHelper.Current);
        adView.AdSize = AdSize.Banner;
        adView.AdUnitId = adUnitId;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        var adParams = new LinearLayout.LayoutParams(
            LayoutParams.WrapContent, LayoutParams.WrapContent);
        adView.LayoutParameters = adParams;

        var testDeviceId = App.AppConfig.AndroidTestDeviceId;
        if (!string.IsNullOrEmpty(testDeviceId))
        {
            var requestConfiguration = new RequestConfiguration.Builder()
                .SetTestDeviceIds(new[] { testDeviceId })
                .Build();
            MobileAds.RequestConfiguration = requestConfiguration;
        }

        adView.LoadAd(new AdRequest.Builder().Build());
        Content = adView;
    }
}
#endif
