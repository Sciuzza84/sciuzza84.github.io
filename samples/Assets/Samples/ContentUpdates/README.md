# Content updates

Install a versioned set of files, transfer only the files that changed, and keep the last usable version after a failed attempt. This original example draws on Cristiano's work on platform-specific content updates for *Don-Ay*. The manifests, payloads and storage are synthetic.

## Try it

Open the **Content Updates** scene. Android and iOS each begin with version 1 installed. Select **Version 2** and **Apply update**. Only the palette transfers. Apply the same manifest again and all three files are reused.

Switch to the other platform and apply version 2 with **Corrupt file** or **Cancel after download** selected. The installed version stays at 1. Select **None** and retry to finish the update. To observe a full first install, clear either installation, select version 1 and apply.

## Read the implementation

[The updater](Runtime/Core/ContentUpdater.cs) compares SHA-256 digests to decide what to reuse and verifies new payloads against the supplied manifest. It creates a staging snapshot and swaps it in only after verification. This demo stores text in memory and simulates errors synchronously. A production service needs a trusted source for its manifest and durable replacement across process restarts.

Run `dotnet run --project Tests/ContentUpdates/ContentUpdates.Checks.csproj -c Release` from the repository root. The [checks](../../../Tests/ContentUpdates/Program.cs) exercise selective transfer, cancellation just before commit, corruption after successful staging, and platform isolation.
