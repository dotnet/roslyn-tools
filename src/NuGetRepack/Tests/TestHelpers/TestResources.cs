 // Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace TestResources
{
    public static class ReleasePackages
    {
        public const string Version = "1.0.0";

        private static byte[] s_A;
        public static byte[] A => ResourceLoader.GetOrCreateResource(ref s_A, NameA);
        public const string NameA = nameof(A) + "." + Version + ".nupkg";

        private static byte[] s_B;
        public static byte[] B => ResourceLoader.GetOrCreateResource(ref s_B, NameB);
        public const string NameB = nameof(B) + "." + Version + ".nupkg";

        private static byte[] s_C;
        public static byte[] C => ResourceLoader.GetOrCreateResource(ref s_C, NameC);
        public const string NameC = nameof(C) + "." + Version + ".nupkg";

        private static byte[] s_D;
        public static byte[] D => ResourceLoader.GetOrCreateResource(ref s_D, NameD);
        public const string NameD = nameof(D) + "." + Version + ".nupkg";
    }

    public static class PreReleasePackages
    {
        public const string Version = "1.0.0-beta";

        private static byte[] s_A;
        public static byte[] A => ResourceLoader.GetOrCreateResource(ref s_A, NameA);
        public const string NameA = nameof(A) + "." + Version + ".nupkg";

        private static byte[] s_B;
        public static byte[] B => ResourceLoader.GetOrCreateResource(ref s_B, NameB);
        public const string NameB = nameof(B) + "." + Version + ".nupkg";

        private static byte[] s_C;
        public static byte[] C => ResourceLoader.GetOrCreateResource(ref s_C, NameC);
        public const string NameC = nameof(C) + "." + Version + ".nupkg";

        private static byte[] s_D;
        public static byte[] D => ResourceLoader.GetOrCreateResource(ref s_D, NameD);
        public const string NameD = nameof(D) + "." + Version + ".nupkg";
    }

    public static class DailyBuildPackages
    {
        public const string Version = "1.0.0-beta-12345-01";

        private static byte[] s_A;
        public static byte[] A => ResourceLoader.GetOrCreateResource(ref s_A, NameA);
        public const string NameA = nameof(A) + "." + Version + ".nupkg";

        private static byte[] s_B;
        public static byte[] B => ResourceLoader.GetOrCreateResource(ref s_B, NameB);
        public const string NameB = nameof(B) + "." + Version + ".nupkg";

        private static byte[] s_C;
        public static byte[] C => ResourceLoader.GetOrCreateResource(ref s_C, NameC);
        public const string NameC = nameof(C) + "." + Version + ".nupkg";

        private static byte[] s_D;
        public static byte[] D => ResourceLoader.GetOrCreateResource(ref s_D, NameD);
        public const string NameD = nameof(D) + "." + Version + ".nupkg";
    }
}
