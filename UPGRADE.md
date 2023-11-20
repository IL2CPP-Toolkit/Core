# Upgrade notes

## Upgrading from 2.0.x to 2.1.x

* Il2cs now tracks process state using mapped memory between host and target processes, allowing it to be reliably detected and managed after the host process exits.
  With this change, the injection will persist in the target process until it is specifically ejected by calling `InjectionHost::Detach`. This will prevent the issues
  seen in previous versions where it becomes impossible to-reinject the process if the multiple host processes are asynchronously trying to manage the target.
* `InjectionHost::RegisterProcess` and `InjectionHost::DeregisterProcess` have been deprecated and removed. They will no longer function.
* All injected processes can now be detached at once for upgrade using `rundll32 Il2CppToolkit.Injection.Host.dll,UnhookAll`.

## Upgrading from 1.x.x to 2.0.x

### Breaking changes for codegen consumers

#### Injection is required

As of v2.0, many features are now implemented via process injection. This means that to function, Il2Cpp-Toolkit must be able to inject a hook DLL which is used to
provide remote process communication (RPC) between your application and the target process. Users may see a request from their OS to allow network traffic for the
target process when it is first injected. This is normal, and users must allow it in order for Il2Cpp-Toolkit to work.

#### Native types

The following primitive types types are no longer required and have been removed. This has been replaced with their built-in .net counterparts:

* `Native__DateTime` -> `System.DateTime`
* `Native__Nullable<T>` -> `System.Nullable<T>`
* `Native__String` -> `System.String`

#### Static Fields

Previously, accessing static fields was limited to classes which had UsageMetadata defined in `global-metadata.dat`, and would not otherwise be emitted.
This limitation is no more, however the way that static fields can be accessed has been changed.

##### Example of original Il2Cpp class

```cs
public class Foo
{
  private static Bar _bar;
}
```

Updated access pattern:

```diff
Il2CsRuntimeContext ctx = getContext();
// access Foo._bar:
- Foo.GetStaticFields(ctx)._bar;
+ Foo._bar.GetValue(ctx);
```

#### Method metadata removed

Method metadata is no longer emitted on class objects, in favor of true method call proxing into the target process via injection.

#### No caching

Previously, when an objects data was first accessed, all of its members were loaded and then cached for subsequent reads. This caused issues with developers who
wanted to re-query the current value for a given member. Rather than making this built-in to the framework, caching is now something the end developer is responsible
for.

Exceptions to this rule include the reference implementations of `Native__` components, such as `Il2CppToolkit.Runtime.Types.corelib.Collections.Generic.Native__Dictionary<K,V>`.

### New features

#### Runtime type substitution via `ITypeFactory`

For types where you want to write your own read/load logic without creating a custom replacement type (e.g. emit as `String` instead of `Native__String`), you can
now declare a type factory.

There are two requirements for type factories:

* `[TypeFactory]` attribute with an argument indicating the type your factory will construct. The codegen will use your class to read values of this type instead
of the its own type generation.
* Class must implement `ITypeFactory`

```cs
[TypeFactory(typeof(string))]
public class StringFactory : ITypeFactory
{
  // ...
}
```
