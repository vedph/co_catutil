using System;
using System.IO;
using System.Reflection;

namespace Catutil.Migration.Test
{
    static internal class TestHelper
    {
        static public Stream LoadResourceStream(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(
                $"Catutil.Migration.Test.Assets.{name}");
        }
    }
}
