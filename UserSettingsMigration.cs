using System;
using System.Diagnostics;

namespace CSVParserTool
{
    internal static class UserSettingsMigration
    {
        public static void UpgradeFromPreviousVersionIfNeeded()
        {
            try
            {
                var settings = Properties.Settings.Default;
                if (!settings.UpgradeRequired)
                    return;

                settings.Upgrade();
                settings.UpgradeRequired = false;
                settings.Save();
            }
            catch (Exception ex)
            {
                // 설정 파일이 잠겼거나 손상된 경우 앱 시작은 계속하고 다음 실행에서 다시 시도합니다.
                Debug.WriteLine("사용자 설정 마이그레이션 실패: " + ex.Message);
            }
        }
    }
}