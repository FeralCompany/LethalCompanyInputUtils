﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using LethalCompanyInputUtils.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalCompanyInputUtils.Api;

public abstract class LcInputActions
{
    private readonly string _jsonPath;
    protected readonly InputActionAsset Asset;
    
    public BepInPlugin Plugin { get; }
    
    protected LcInputActions()
    {
        Asset = ScriptableObject.CreateInstance<InputActionAsset>();

        Plugin = Assembly.GetCallingAssembly().GetBepInPlugin() ?? throw new InvalidOperationException();
        
        var modGuid = Plugin.GUID;
        _jsonPath = Path.Combine(FsUtils.ControlsDir, $"{modGuid}.json");

        var mapBuilder = new InputActionMapBuilder(modGuid);

        var props = GetType().GetProperties();

        var inputProps = new Dictionary<PropertyInfo, InputActionAttribute>();
        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<InputActionAttribute>();
            if (attr is null)
                continue;

            if (prop.PropertyType != typeof(InputAction))
                continue;
            
            var actionBuilder = mapBuilder.NewActionBinding();

            actionBuilder
                .WithActionName(attr.Action)
                .WithActionType(attr.ActionType)
                .WithBindingName(attr.Name)
                .WithKbmPath(attr.KbmPath)
                .WithGamepadPath(attr.GamepadPath)
                .Finish();

            inputProps[prop] = attr;
        }

        Asset.AddActionMap(mapBuilder.Build());
        Asset.Enable();

        foreach (var (prop, attr) in inputProps)
            prop.SetValue(this, Asset.FindAction(attr.Action));
    }

    public void Enable()
    {
        Asset.Enable();
    }

    public void Disable()
    {
        Asset.Disable();
    }

    internal void Save()
    {
        File.WriteAllText(_jsonPath, Asset.SaveBindingOverridesAsJson());
    }

    internal void Load()
    {
        Asset.LoadBindingOverridesFromJson(_jsonPath);
    }
}