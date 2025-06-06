// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveUI.Tests;

/// <summary>
/// A routable view.
/// </summary>
public class RoutableFooCustomView : IViewFor<IRoutableFooViewModel>
{
    /// <inheritdoc/>
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (IRoutableFooViewModel?)value;
    }

    /// <inheritdoc/>
    public IRoutableFooViewModel? ViewModel { get; set; }
}
