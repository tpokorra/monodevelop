﻿//
// PackageUpdateStartupHandler.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using ICSharpCode.PackageManagement;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement.Commands
{
	public class PackageUpdatesStartupHandler : CommandHandler
	{
		protected override void Run ()
		{
			PackageManagementServices.ProjectService.SolutionLoaded += SolutionLoaded;
			PackageManagementServices.ProjectService.SolutionUnloaded += SolutionUnloaded;
		}

		void SolutionLoaded (object sender, EventArgs e)
		{
			PackageManagementServices.UpdatedPackagesInSolution.Clear ();

			if (!PackageManagementServices.Options.IsCheckForPackageUpdatesOnOpeningSolutionEnabled)
				return;

			DispatchService.BackgroundDispatch (() => {
				var checker = new PackageUpdateChecker ();
				checker.Run ();
			});
		}

		void SolutionUnloaded (object sender, EventArgs e)
		{
			PackageManagementServices.UpdatedPackagesInSolution.Clear ();
		}
	}
}
