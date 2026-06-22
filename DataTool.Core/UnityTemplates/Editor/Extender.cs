// DataTool 패키지 포함 — export 시 Assets/_Game/DataTables/Scripts/Editor 로 복사됩니다.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace PJDev.Data.Editor
{
  [InitializeOnLoad]
  public static class Extender
  {
    private const string UNITASK_NAME = "com.cysharp.unitask";

    private const string UNITASK_URL =
      "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask";

    private static AddRequest addRequest;

    static Extender()
    {
      EditorApplication.delayCall += EnsureUniTaskInstalled;
    }

    private static void EnsureUniTaskInstalled()
    {
      if (IsPackageInstalled(UNITASK_NAME))
        return;

      addRequest = Client.Add(UNITASK_URL);
      EditorApplication.update += WaitForAddRequest;
    }

    private static void WaitForAddRequest()
    {
      if (addRequest == null || !addRequest.IsCompleted)
        return;

      EditorApplication.update -= WaitForAddRequest;

      if (addRequest.Status == StatusCode.Success)
        Debug.Log("[PJDev.Data] UniTask package installed.");
      else
        Debug.LogWarning("[PJDev.Data] UniTask install failed: " + addRequest.Error?.message);
    }

    private static bool IsPackageInstalled(string packageName)
    {
      foreach (PackageInfo package in PackageInfo.GetAllRegisteredPackages())
      {
        if (package.name == packageName)
          return true;
      }

      return false;
    }
  }
}
#endif
