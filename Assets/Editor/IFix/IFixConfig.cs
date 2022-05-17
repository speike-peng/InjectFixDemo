
using System;
using System.Reflection;
using System.Collections.Generic;
using IFix;

//在注入阶段使用；配置类，里面存储的是一些注入时需要注入或过滤的东西。
[Configure]
public class IFixConfig
{
    public static string[] assemblys = HotfixHelper.assemblys;

    // 有命名空间的类默认不注入，需要注入的请在下面指定(使用StartsWith判定)
    public static string[] namespaceWhileList = new[]
    {
        "",
    };

    //在注入阶段使用；用来存储所有你认为将来可能会需要修复的类的集合。该标签和[IFix.Patch]有关联，因为如果发现某个函数需要修复，直接打上[IFix.Patch]标签就可以了，但是前提是，这个需要修复的函数的类必须在[IFix]下。
    [IFix]
    static IEnumerable<Type> hotfixInject
    {
        get { return Assembly_CSharp_Class(); }
    }

    public static List<Type> Assembly_CSharp_Class()
    {
        List<Type> types = new List<Type>();
        foreach (var assembly in assemblys)
        {
            types.AddRange(Assembly.Load(assembly).GetExportedTypes());
        }

        List<Type> result = new List<Type>();
        foreach (var type in types)
        {
            // 过滤匿名类
            if (type.Name.Contains("<"))
                continue;

            if (type.Namespace != null)
            {
                bool inWhileList = false;
                
                foreach (var s in namespaceWhileList)
                {
                    if (type.Namespace.StartsWith(s))
                    {
                        inWhileList = true;
                        break;
                    }
                }
                
                if (!inWhileList)
                    continue;
            }
            
            result.Add(type);
        }

        return result;
    }
}
