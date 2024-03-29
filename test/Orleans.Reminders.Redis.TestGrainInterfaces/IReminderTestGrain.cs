﻿using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace Orleans.Reminders.Redis.TestGrainInterfaces
{
    public interface IReminderTestGrain : IGrainWithStringKey
    {
        Task<GrainReference> GetReference();
    }
}
