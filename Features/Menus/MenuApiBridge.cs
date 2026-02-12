using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CounterStrikeSharp.API.Core;

namespace ModeManager;

internal sealed class MenuApiBridge
{
    private static readonly string[] _menuApiInterfaceCandidates =
    {
        "MenuManagerAPI.Shared.IMenuApi",
        "MenuManagerAPI.Shared.IMenuAPI"
    };
    private static readonly string[] _menuApiCloseMethodCandidates =
    {
        "CloseMenu",
        "CloseActiveMenu"
    };
    private static readonly string[] _menuTypeButtonCandidates =
    {
        "Button",
        "Buttons"
    };

    private readonly Action<string> _logInfo;
    private readonly Func<MessageKey, string> _msg;

    private object? _menuApiCapability;
    private Type? _menuApiInterfaceType;
    private object? _menuApi;
    private bool _menuApiAvailableLogged;

    public MenuApiBridge(Action<string> logInfo, Func<MessageKey, string> msg)
    {
        _logInfo = logInfo;
        _msg = msg;
    }

    public bool TryResolve()
    {
        if (_menuApi != null)
            return true;

        if (!TryCreateMenuApiCapability())
            return false;

        if (_menuApiCapability == null)
            return false;

        try
        {
            var getMethod = _menuApiCapability.GetType().GetMethod(
                "Get",
                BindingFlags.Public | BindingFlags.Instance);
            if (getMethod == null)
                return false;

            _menuApi = getMethod.Invoke(_menuApiCapability, null);
        }
        catch
        {
            _menuApi = null;
        }

        if (_menuApi == null)
            return false;

        if (_menuApiAvailableLogged)
            return true;

        _logInfo(_msg(MessageKey.LogMenuApiAvailable));
        _menuApiAvailableLogged = true;
        return true;
    }

    public bool TryCreateMenu(string title, out object? menu)
    {
        menu = null;
        if (!TryResolve() || _menuApi == null)
            return false;

        var getMenu = _menuApi
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name.Equals("GetMenu", StringComparison.Ordinal))
            .Where(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length >= 1 && parameters[0].ParameterType == typeof(string);
            })
            .OrderByDescending(method => method
                .GetParameters()
                .Any(parameter => TryCreateForcedButtonMenuTypeArgument(parameter, out _)))
            .ThenByDescending(method => method.GetParameters().Length)
            .FirstOrDefault();

        if (getMenu == null)
            return false;

        var parameters = getMenu.GetParameters();
        var args = new object?[parameters.Length];
        args[0] = title;

        for (var i = 1; i < parameters.Length; i++)
        {
            if (TryCreateForcedButtonMenuTypeArgument(parameters[i], out var forcedButtonMenuType))
            {
                args[i] = forcedButtonMenuType;
                continue;
            }

            args[i] = GetDefaultArgumentValue(parameters[i]);
        }

        try
        {
            menu = getMenu.Invoke(_menuApi, args);
            return menu != null;
        }
        catch
        {
            menu = null;
            return false;
        }
    }

    public bool TryAddMenuOption(object menu, string text, Action<CCSPlayerController> onSelect)
    {
        var addOption = menu
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name.Equals("AddMenuOption", StringComparison.Ordinal))
            .Where(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length >= 2 &&
                       parameters[0].ParameterType == typeof(string) &&
                       typeof(Delegate).IsAssignableFrom(parameters[1].ParameterType);
            })
            .OrderByDescending(method => method.GetParameters().Length)
            .FirstOrDefault();

        if (addOption == null)
            return false;

        var parameters = addOption.GetParameters();
        var callback = BuildMenuCallbackDelegate(parameters[1].ParameterType, onSelect);
        if (callback == null)
            return false;

        var args = new object?[parameters.Length];
        args[0] = text;
        args[1] = callback;

        for (var i = 2; i < parameters.Length; i++)
            args[i] = GetDefaultArgumentValue(parameters[i]);

        try
        {
            addOption.Invoke(menu, args);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryOpenMenu(object menu, CCSPlayerController player)
    {
        if (!TryResolve() || _menuApi == null)
            return false;

        try
        {
            var openMenuMethod = _menuApi
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(method =>
                {
                    if (!method.Name.Equals("OpenMenu", StringComparison.Ordinal))
                        return false;

                    var parameters = method.GetParameters();
                    return parameters.Length == 2 &&
                           parameters[0].ParameterType.IsInstanceOfType(menu) &&
                           parameters[1].ParameterType.IsAssignableFrom(typeof(CCSPlayerController));
                });

            if (openMenuMethod != null)
            {
                openMenuMethod.Invoke(_menuApi, new object?[] { menu, player });
                return true;
            }

            var openMethod = menu
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(method =>
                {
                    if (!method.Name.Equals("Open", StringComparison.Ordinal))
                        return false;

                    var parameters = method.GetParameters();
                    return parameters.Length == 1 &&
                           parameters[0].ParameterType.IsAssignableFrom(typeof(CCSPlayerController));
                });

            if (openMethod == null)
                return false;

            openMethod.Invoke(menu, new object?[] { player });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryCloseMenuForPlayer(CCSPlayerController player)
    {
        if (!TryResolve() || _menuApi == null)
            return false;

        try
        {
            var closeMethod = _menuApi
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => _menuApiCloseMethodCandidates.Contains(method.Name, StringComparer.Ordinal))
                .FirstOrDefault(method =>
                {
                    var parameters = method.GetParameters();
                    return parameters.Length == 1 &&
                           parameters[0].ParameterType.IsAssignableFrom(typeof(CCSPlayerController));
                });

            if (closeMethod == null)
                return false;

            closeMethod.Invoke(_menuApi, new object?[] { player });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryCreateMenuApiCapability()
    {
        if (_menuApiCapability != null)
            return true;

        if (!TryResolveMenuApiInterfaceType(out var interfaceType))
            return false;

        try
        {
            var pluginCapabilityType = Type.GetType(
                "CounterStrikeSharp.API.Core.Capabilities.PluginCapability`1, CounterStrikeSharp.API");
            if (pluginCapabilityType == null)
                return false;

            var typedCapability = pluginCapabilityType.MakeGenericType(interfaceType);
            _menuApiCapability = Activator.CreateInstance(typedCapability, "menu:api");
            return _menuApiCapability != null;
        }
        catch
        {
            _menuApiCapability = null;
            return false;
        }
    }

    private bool TryResolveMenuApiInterfaceType(out Type interfaceType)
    {
        if (_menuApiInterfaceType != null)
        {
            interfaceType = _menuApiInterfaceType;
            return true;
        }

        foreach (var candidate in _menuApiInterfaceCandidates)
        {
            var resolved = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(candidate, false, false))
                .FirstOrDefault(type => type != null);

            if (resolved != null)
            {
                _menuApiInterfaceType = resolved;
                interfaceType = resolved;
                return true;
            }
        }

        interfaceType = null!;
        return false;
    }

    private static Delegate? BuildMenuCallbackDelegate(Type callbackType, Action<CCSPlayerController> onSelect)
    {
        try
        {
            var invokeMethod = callbackType.GetMethod("Invoke");
            if (invokeMethod == null)
                return null;

            var callbackParameters = invokeMethod.GetParameters();
            if (callbackParameters.Length == 0)
                return null;

            var parameterExpressions = callbackParameters
                .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name ?? "arg"))
                .ToArray();

            var playerParameterIndex = Array.FindIndex(
                callbackParameters,
                parameter =>
                {
                    var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                    return parameterType.IsAssignableFrom(typeof(CCSPlayerController));
                });

            if (playerParameterIndex < 0)
                return null;

            var playerExpr = Expression.Convert(parameterExpressions[playerParameterIndex], typeof(CCSPlayerController));
            var callbackExpr = Expression.Constant(onSelect);
            var handlerMethod = typeof(MenuApiBridge).GetMethod(
                nameof(ExecuteSelection),
                BindingFlags.Static | BindingFlags.NonPublic);

            if (handlerMethod == null)
                return null;

            var handlerCall = Expression.Call(handlerMethod, playerExpr, callbackExpr);
            Expression body = handlerCall;
            if (invokeMethod.ReturnType != typeof(void))
                body = Expression.Block(handlerCall, Expression.Default(invokeMethod.ReturnType));

            return Expression.Lambda(callbackType, body, parameterExpressions).Compile();
        }
        catch
        {
            return null;
        }
    }

    private static void ExecuteSelection(CCSPlayerController player, Action<CCSPlayerController> onSelect)
    {
        if (player == null || !player.IsValid)
            return;

        onSelect(player);
    }

    private static bool TryCreateForcedButtonMenuTypeArgument(ParameterInfo parameter, out object? argument)
    {
        argument = null;

        var rawType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (!rawType.IsEnum)
            return false;

        var enumNames = Enum.GetNames(rawType);
        var buttonName = enumNames.FirstOrDefault(
            name => _menuTypeButtonCandidates.Any(
                candidate => name.Equals(candidate, StringComparison.OrdinalIgnoreCase)));

        if (buttonName == null)
            return false;

        argument = Enum.Parse(rawType, buttonName, ignoreCase: true);
        return true;
    }

    private static object? GetDefaultArgumentValue(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;

        var parameterType = parameter.ParameterType;
        if (Nullable.GetUnderlyingType(parameterType) != null)
            return null;

        return parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
    }
}
