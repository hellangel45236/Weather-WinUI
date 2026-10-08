using System;
using Windows.System.Power;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ quản lý và tối ưu năng lượng Laptop / Battery Saver.
/// Tự động lắng nghe trạng thái tiết kiệm pin của Windows để điều tiết hiệu ứng chuyển động và timer.
/// </summary>
public class PowerManagementService
{
    public event EventHandler<bool>? PowerSavingStateChanged;

    public bool IsPowerSavingActive
    {
        get
        {
            try
            {
                // 1. Chế độ Battery Saver của Windows đang bật
                if (PowerManager.EnergySaverStatus == EnergySaverStatus.On)
                {
                    return true;
                }

                // 2. Máy tính xách tay đang chạy bằng pin (Discharging) và dưới 35% pin
                if (PowerManager.BatteryStatus == BatteryStatus.Discharging && PowerManager.RemainingChargePercent < 35)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }
    }

    public int RemainingBatteryPercent
    {
        get
        {
            try
            {
                return PowerManager.RemainingChargePercent;
            }
            catch
            {
                return 100;
            }
        }
    }

    public bool IsOnBattery
    {
        get
        {
            try
            {
                return PowerManager.BatteryStatus == BatteryStatus.Discharging;
            }
            catch
            {
                return false;
            }
        }
    }

    public PowerManagementService()
    {
        try
        {
            PowerManager.EnergySaverStatusChanged += (s, e) => TriggerStateChange();
            PowerManager.BatteryStatusChanged += (s, e) => TriggerStateChange();
            PowerManager.RemainingChargePercentChanged += (s, e) => TriggerStateChange();
        }
        catch { }
    }

    private void TriggerStateChange()
    {
        PowerSavingStateChanged?.Invoke(this, IsPowerSavingActive);
    }
}
