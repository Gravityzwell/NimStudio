
using System;

namespace NimStudio.NimStudio {

    static class GuidList {
        public const string NSPkgGUIDStr = "ef6a91e8-dd7d-48aa-a77c-69b7b8e9229a";
        public const string NSMenuCmdSubGUIDStr = "91aa9d6a-53f9-4ffb-8534-a7733de397c8";
        public const string NSMenuCmdTopGUIDStr = "5e97f4b1-b272-4aa7-ab7a-d137fd9662f7";

        public static readonly Guid NSMenuCmdSubGUID = new Guid(NSMenuCmdSubGUIDStr);
        public static readonly Guid NSMenuCmdTopGUID = new Guid(NSMenuCmdSubGUIDStr);

    };
}