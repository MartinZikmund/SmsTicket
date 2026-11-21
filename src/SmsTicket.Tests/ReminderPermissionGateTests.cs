using SmsTicket.Services.AppSettings;
using SmsTicket.Services.Reminders;

namespace SmsTicket.Tests;

[TestClass]
public class ReminderPermissionGateTests
{
    [TestMethod]
    public async Task FirstCall_AsksForPermission()
    {
        FakeReminderPermission permission = new();
        ReminderPermissionGate gate = new(new AppSettings(new InMemorySettingsService()), permission);

        await gate.EnsureAskedAsync();

        Assert.AreEqual(1, permission.Requests);
    }

    [TestMethod]
    public async Task LaterCalls_DoNotAskAgain()
    {
        FakeReminderPermission permission = new();
        InMemorySettingsService settings = new();

        await new ReminderPermissionGate(new AppSettings(settings), permission).EnsureAskedAsync();
        await new ReminderPermissionGate(new AppSettings(settings), permission).EnsureAskedAsync();

        Assert.AreEqual(1, permission.Requests);
    }

    [TestMethod]
    public async Task Denied_IsNotAskedAgain()
    {
        FakeReminderPermission permission = new() { Grant = false };
        InMemorySettingsService settings = new();

        await new ReminderPermissionGate(new AppSettings(settings), permission).EnsureAskedAsync();
        await new ReminderPermissionGate(new AppSettings(settings), permission).EnsureAskedAsync();

        Assert.AreEqual(1, permission.Requests);
    }

    private sealed class FakeReminderPermission : IReminderPermission
    {
        public bool Grant { get; init; } = true;

        public int Requests { get; private set; }

        public Task<bool> RequestAsync()
        {
            Requests++;
            return Task.FromResult(Grant);
        }
    }
}
