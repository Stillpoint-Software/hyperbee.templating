using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Hyperbee.Templating.Compiler;

internal class DynamicAssemblyLoadContext : AssemblyLoadContext
{
    public DynamicAssemblyLoadContext( string[] preloadPaths ) : base( isCollectible: true )
    {
        PreloadCommonDependencies( preloadPaths );
    }

    public DynamicAssemblyLoadContext( ImmutableArray<MetadataReference> metadataReferences ) : base( isCollectible: true )
    {
        // Preload assemblies from metadata references

        var preloadPaths = metadataReferences.OfType<PortableExecutableReference>()
            .Select( reference => reference.FilePath )
            .Where( path => !string.IsNullOrEmpty( path ) && File.Exists( path ) )
            .Distinct()
            .ToArray();

        PreloadCommonDependencies( preloadPaths );
    }

    private void PreloadCommonDependencies( string[] preloadPaths )
    {
        if ( preloadPaths == null )
            return;

        foreach ( var path in preloadPaths )
        {
            if ( !File.Exists( path ) )
                continue;

            var assemblyName = AssemblyName.GetAssemblyName( path );

            if ( TryGetExistingAssembly( assemblyName.Name, out _ ) )
                continue;

            LoadFromAssemblyPath( path );
        }
    }

    protected override Assembly Load( AssemblyName assemblyName )
    {
        return TryGetExistingAssembly( assemblyName.Name, out var assembly ) ? assembly : null;
    }

    private bool TryGetExistingAssembly( string assemblyName, out Assembly assembly )
    {
        // Check for assembly in the current context
        // If not found, check in the default context
        // If still not found, return null
        //
        // This is to prevent loading the same assembly multiple times

        if ( TryGetAssemblyFromContext( this, assemblyName, out assembly ) ||
             TryGetAssemblyFromContext( Default, assemblyName, out assembly ) )
            return true;

        assembly = null;
        return false;

        static bool TryGetAssemblyFromContext( AssemblyLoadContext context, string assemblyName, out Assembly assembly )
        {
            assembly = context.Assemblies.FirstOrDefault( a => a.GetName().Name == assemblyName );
            return assembly != null;
        }
    }
}
