using System;
using UnityEngine;

namespace Room6.TSearch.Editor
{
    [Flags]
    public enum ResultType
    {
        Assets          = 1 << 0,
        Hierarchy       = 1 << 1,
        MenuCommand     = 1 << 2,
        History         = 1 << 3,
        TextInHierarchy = 1 << 4,
        All             = Assets | MenuCommand,
    }

    public static class ResultTypeExtensions
    {
        public static bool IsAssets(this ResultType type)
        {
            return (type & ResultType.Assets) != 0;
        }

        public static bool IsMenuCommand(this ResultType type)
        {
            return (type & ResultType.MenuCommand) != 0;
        }

        public static int ToIndex(this ResultType type)
        {
            return Mathf.RoundToInt(Mathf.Log((int)type, 2));
        }
    }
}