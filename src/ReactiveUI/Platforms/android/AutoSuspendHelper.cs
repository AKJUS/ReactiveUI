﻿// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Android.App;
using Android.OS;

namespace ReactiveUI;

/// <summary>
/// Helps manage android application lifecycle events.
/// </summary>
#if NET6_0_OR_GREATER
[RequiresDynamicCode("The method uses reflection and will not work in AOT environments.")]
[RequiresUnreferencedCode("The method uses reflection and will not work in AOT environments.")]
#endif
public class AutoSuspendHelper : IEnableLogger, IDisposable
{
    private readonly Subject<Bundle?> _onCreate = new();
    private readonly Subject<Unit> _onRestart = new();
    private readonly Subject<Unit> _onPause = new();
    private readonly Subject<Bundle?> _onSaveInstanceState = new();

    private bool _disposedValue; // To detect redundant calls

    /// <summary>
    /// Initializes static members of the <see cref="AutoSuspendHelper"/> class.
    /// </summary>
    static AutoSuspendHelper() => AppDomain.CurrentDomain.UnhandledException += (o, e) => UntimelyDemise.OnNext(Unit.Default);

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoSuspendHelper"/> class.
    /// </summary>
    /// <param name="hostApplication">The host application.</param>
    public AutoSuspendHelper(Application hostApplication) // TODO: Create Test
    {
        hostApplication?.RegisterActivityLifecycleCallbacks(new ObservableLifecycle(this));

        _onCreate.Merge(_onSaveInstanceState).Subscribe(x => LatestBundle = x);

        RxApp.SuspensionHost.IsLaunchingNew = _onCreate.Where(x => x is null).Select(_ => Unit.Default);
        RxApp.SuspensionHost.IsResuming = _onCreate.Where(x => x is not null).Select(_ => Unit.Default);
        RxApp.SuspensionHost.IsUnpausing = _onRestart;
        RxApp.SuspensionHost.ShouldPersistState = _onPause.Select(_ => Disposable.Empty);
        RxApp.SuspensionHost.ShouldInvalidateState = UntimelyDemise;
    }

    /// <summary>
    /// Gets a subject to indicate whether the application has untimely dismissed.
    /// </summary>
    public static Subject<Unit> UntimelyDemise { get; } = new();

    /// <summary>
    /// Gets or sets the latest bundle.
    /// </summary>
    public static Bundle? LatestBundle { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        // Do not change this code. Put clean up code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the items inside the class.
    /// </summary>
    /// <param name="disposing">If we are disposing of managed objects or not.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _onCreate?.Dispose();
            _onPause?.Dispose();
            _onRestart?.Dispose();
            _onSaveInstanceState?.Dispose();
        }

        _disposedValue = true;
    }

    private class ObservableLifecycle(AutoSuspendHelper @this) : Java.Lang.Object, Application.IActivityLifecycleCallbacks
    {
        public void OnActivityCreated(Activity? activity, Bundle? savedInstanceState) => @this._onCreate.OnNext(savedInstanceState);

        public void OnActivityResumed(Activity? activity) => @this._onRestart.OnNext(Unit.Default);

        public void OnActivitySaveInstanceState(Activity? activity, Bundle? outState)
        {
            // NB: This is so that we always have a bundle on OnCreate, so that
            // we can tell the difference between created from scratch and resume.
            outState?.PutString("___dummy_value_please_create_a_bundle", "VeryYes");

            @this._onSaveInstanceState.OnNext(outState);
        }

        public void OnActivityPaused(Activity? activity) => @this._onPause.OnNext(Unit.Default);

        public void OnActivityDestroyed(Activity? activity)
        {
        }

        public void OnActivityStarted(Activity? activity)
        {
        }

        public void OnActivityStopped(Activity? activity)
        {
        }
    }
}
