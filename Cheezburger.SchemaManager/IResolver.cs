using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Cheezburger.SchemaManager.Structure;

namespace Cheezburger.SchemaManager
{
    public interface IResolver
    {
        Schema Resolve(string name);
    }

    public abstract class StreamResolverBase
    {
        protected string _path;

        public virtual Schema Resolve(string name)
        {
            var serializer = new XmlSerializer(typeof(Schema));
            Schema result;

            using (var stream = ResolveStream(name))
            {
                if (stream == null)
                    throw new FileNotFoundException("Unable to resolve schema: " + name, name);

                using (var reader = new StreamReader(stream))
                    result = (Schema)serializer.Deserialize(reader);
            }

            if (string.IsNullOrEmpty(result.Name))
                result.Name = name.Substring(_path.Length);

            return result;
        }

        protected abstract Stream ResolveStream(string name);
    }

    public class EmbeddedResourceResolver : StreamResolverBase, IResolver
    {
        private readonly Assembly _assembly;
        private readonly string _ns;

        public EmbeddedResourceResolver(Assembly assembly, string path, string @namespace)
        {
            _assembly = assembly;
            _path = path;
            _ns = @namespace;
        }

        protected override Stream ResolveStream(string name)
        {
            return _assembly.GetManifestResourceStream(_ns + "." + name);
        }
    }

    public class FileResolver : StreamResolverBase, IResolver
    {
        public FileResolver(string path)
        {
            _path = path;
        }

        protected override Stream ResolveStream(string name)
        {
            return new FileStream(Path.Combine(_path, name), FileMode.Open);
        }
    }
}
