# Content updates

Install a versioned set of files, transfer only the files that changed, and keep the last usable version after a failed attempt. This original example draws on Cristiano's work on platform-specific content updates for *Don-Ay*. The manifests, payloads and storage are synthetic.

## Try it

Open the **Content Updates** scene. Android and iOS each begin with version 1 installed. Select **Version 2** and **Apply update**. Only the palette transfers. Apply the same manifest again and all three files are reused.

Switch to the other platform and apply version 2 with **Corrupt file** or **Cancel after download** selected. The installed version stays at 1. Select **None** and retry to finish the update. To observe a full first install, clear either installation, select version 1 and apply.

## Keep the installed version after partial staging

1. Install **Version 2**, then select **Version 3** on the same platform. The board and rules need downloads, and the palette is reusable.
2. Select **Corrupt file** and apply. The board passes verification and stages before the rules file fails. The result reports one verified download staged, the header still shows **Installed: v2**, and the board still needs a download.
3. Select **None** and apply again. Both changed files transfer, the palette is reused, and **Installed: v3** confirms the commit.

## Read the implementation

[The updater](Runtime/Core/ContentUpdater.cs) compares SHA-256 digests to decide what to reuse and verifies new payloads against the supplied manifest. It creates a staging snapshot and swaps it in only after verification. This demo stores text in memory and simulates errors synchronously. A production service needs a trusted source for its manifest and durable replacement across process restarts.

Run `dotnet run --project Tests/ContentUpdates/ContentUpdates.Checks.csproj -c Release` from the Unity project directory (`samples/` in the portfolio repository). The [checks](../../../Tests/ContentUpdates/Program.cs) cover selective transfer, cancellation, the version 3 partial staging failure and retry, and platform isolation.
