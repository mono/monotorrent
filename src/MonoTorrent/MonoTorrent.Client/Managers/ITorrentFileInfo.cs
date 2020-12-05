﻿//
// IFile.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2020 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System.Diagnostics;
using System.Threading;

namespace MonoTorrent.Client
{

    public interface ITorrentFileInfo : ITorrentFile
    {
        // FIXME: make BitField readonly.
        BitField BitField { get; }
        string FullPath { get; }
        Priority Priority { get; set; }

        // FIXME: Make this internal.
        SemaphoreSlim Locker { get; }

        (int startPiece, int endPiece) GetSelector ();
    }

    public static class ITorrentFileInfoExtensions
    {
        public static long BytesDownloaded (this ITorrentFileInfo info)
            => (long) (info.BitField.PercentComplete * info.Length / 100.0);

        [Conditional ("DEBUG")]
        internal static void ThrowIfNotLocked(this ITorrentFileInfo info)
        {
            if (info.Locker.CurrentCount > 0)
                throw new System.InvalidOperationException ("File should have been locked before it was accessed");
        }
    }
}
