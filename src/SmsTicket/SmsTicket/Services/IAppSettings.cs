using System;
using System.Collections.Generic;
using System.Text;

namespace SmsTicket.Services.AppSettings;

public interface IAppSettings
{
    bool FirstTimeUse { get; set; }
}
