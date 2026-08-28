# Mobile Console Kit
Mobile Console Kit is a Unity [Asset Store](https://assetstore.unity.com/packages/tools/gui/mobile-console-kit-128118) that help you not just viewing logs in mobile devices, but helping you monitor, do experiment and speed up development time.

> Mobile Console Kit depends on TextMesh Pro for high performance and customization. You need to install `TextMesh Pro` and import `Essential Resources` before installing Mobile Console Kit

```
Install via UPM: https://github.com/pixeption/MobileConsoleKit.git
```

# Key features
- Enable/Disable Mobile Console with a single click (no trace left)
- Highly optimization, can handle dozen of thousands of logs
- Supports basic functions like filter log by type, collapse
- Supports log channel
- Search log with Regex
- Share log via Native Share
- Send a bug report to your own bug tracker (see [Bug Report](#bug-report))
- Unified portrait and landscape UI, resizable window and adjustable background transparency
- User-defined Setting and Commands, allow you to create your own tools
- Comes with powerful tools including: 
  - Application & Device info
  - Search GameObject by name/tag/component
  - Inspect PlayerPrefs
  - Inspect persistent data

# Table of content
For more detail on each features, please see the wiki below:
## Basic
- [Enable/Disable Mobile Console](https://github.com/pixeption/MobileConsoleKit/wiki/Enable-Console)
- [Tool Overview](https://github.com/pixeption/MobileConsoleKit/wiki)
- [Log Channel](https://github.com/pixeption/MobileConsoleKit/wiki/Log-Channel)
- [Share Log](https://github.com/pixeption/MobileConsoleKit/wiki/Share-Log)
- [Setting & Executable Commands](https://github.com/pixeption/MobileConsoleKit/wiki/Commands)
- [Built-in Commands (Search Game Object, PlayerPrefs Inspector, Persistent Data Inspector)](https://github.com/pixeption/MobileConsoleKit/wiki/Built-in-Commands)

## Advance
- [Built-in Log](https://github.com/pixeption/MobileConsoleKit/wiki/Built-in-Log)
- [Console Settings](https://github.com/pixeption/MobileConsoleKit/wiki/Console-Settings)
- [View Builder](https://github.com/pixeption/MobileConsoleKit/wiki/View-Builder)

# Bug Report
Next to the share button there is a bug button that opens a report form: title, reporter, the same log options as the share window, plus a screenshot of the game. Anything tracker specific, like severity, is a custom option on your own `BugReporter`.

The package contains only the common part. The integration with a concrete bug tracker is written on the project side, because every tracker has its own endpoint, headers and fields. The bug button stays hidden until at least one integration is registered.

```csharp
public class MyTrackerBugReporter : BugReporter
{
    // Tracker specific options, rendered in the form and persisted in PlayerPrefs
    public class Options : Command
    {
        public string endpoint = "https://example.com/api/bug-reports";
        public string projectKey = "GAME";
        public MySeverity severity = MySeverity.Normal; // your own enum, defined on the project side
    }

    readonly Options _options = new Options();

    public override string name { get { return "QA Tracker"; } }
    public override Command customOptions { get { return _options; } }

    public override IEnumerator Send(BugReport report, Action<BugReportResult> onComplete)
    {
        // report.title, report.context, _options.severity,
        // report.logText, report.selectedLog, report.attachments (screenshot + log file)
        using (UnityWebRequest request = UnityWebRequest.Post(_options.endpoint, BuildForm(report)))
        {
            yield return request.SendWebRequest();
            onComplete(request.result == UnityWebRequest.Result.Success
                ? BugReportResult.Success()
                : BugReportResult.Failure(request.error));
        }
    }
}
```

Register it once at startup, and add whatever makes a bug reproducible in your game:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void Initialize()
{
    BugReportContext.Register("player.id", () => Profile.Current.Id);
    BugReportContext.Register("game.level", () => LevelManager.Current.Name);

    BugReportService.Register(new MyTrackerBugReporter());
}
```

Several integrations can be registered at the same time, the bug button then opens a picker first.

A ready to read example lives in `Assets/Scripts/BugReport` of the development project.

# Migrate from v1.0 to v2.0
As there are many parts of the tool have been rewritten, it's recommended that you need to delete the whole **Mobile Console** folder before import the new one

# Video Demo
- [v1.0 Introduction](https://youtu.be/IGDuiXixl1Q)
- [v2.0 Showcase](https://youtu.be/aQfkZ8-pjQU)

# Demo APK
- [PlayStore](https://play.google.com/store/apps/details?id=com.pixeption.mck.showcase)
- [Github](https://github.com/pixeption/MobileConsoleKit/releases/download/demo/v2.0_demo.apk)

# Preview Images
## Tool Overview
![](https://i.imgur.com/8tO6mb1.png) ![](https://i.imgur.com/MRW3zcq.png) ![](https://i.imgur.com/UCqadL7.png)

## Share Log View
![](https://i.imgur.com/LQ0wEzE.png)

## Setting View
![](https://i.imgur.com/Rc0TGhi.png)

## Command View
![](https://i.imgur.com/Y8flnun.png)

## Log Channel Filter
![](https://i.imgur.com/lLOx3Y6.png)

## App and Device Info
![](https://i.imgur.com/tmz2UVn.png)

## Persistent Data Inspector
![](https://i.imgur.com/Z3jf5Bt.png) ![](https://i.imgur.com/RiSjjGh.png)

## PlayerPrefs Inspector
![](https://i.imgur.com/dP0e2rx.png)

## Search GameObject
![](https://i.imgur.com/qpqgMMW.png) ![](https://i.imgur.com/3ueQ451.png)
