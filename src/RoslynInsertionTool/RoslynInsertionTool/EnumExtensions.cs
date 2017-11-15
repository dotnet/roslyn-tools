// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace ExtensionMethods
{
    public static class EnumExtensions
    {
        public static string GetEnumName<TEnum>(this TEnum @enum)
            where TEnum : struct
            => Enum.GetName(typeof(TEnum), @enum);
    }
}
