// Guids.cs
// MUST match guids.h
using System;

namespace NimStudio.NimStudio
{
    static class GuidList
    {
        public const string guidNimStudioPkgString = "ef6a91e8-dd7d-48aa-a77c-69b7b8e9229a";
        public const string guidNimStudioCmdSetString = "91aa9d6a-53f9-4ffb-8534-a7733de397c8";

        public static readonly Guid guidNimStudioCmdSet = new Guid(guidNimStudioCmdSetString);
    };
}