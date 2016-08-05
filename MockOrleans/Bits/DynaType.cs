using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace MockOrleans
{
    public static class DynaType
    {

        #region Static Stuff

        static AssemblyBuilder _blAsm;
        static ModuleBuilder _blMod;
        static int _iCounter = 1;

        static DynaType() {

            var asmName = new AssemblyName($"DynaType_{Guid.NewGuid()}");

            _blAsm = AppDomain.CurrentDomain.DefineDynamicAssembly(
                asmName,
                AssemblyBuilderAccess.RunAndSave
                );
            
            _blMod = _blAsm.DefineDynamicModule(asmName.Name, $"{asmName.Name}.dll");
            
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        //private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
        //    if(args.Name == _blMod.Assembly.FullName || args.Name == _blMod.Assembly.GetName().Name) {
        //        return _blMod.Assembly;
        //    }
            
        //    return null;
        //}


        public static Assembly Assembly {
            get { return _blMod.Assembly; }
        }


        #endregion


        #region ILGenerator extensions

        public static void Emit(this ILGenerator il, OpCode opcode, IFieldElement el) {
            var m = el as IMemberInfoSource;
            var fieldInfo = m.MemberInfo as FieldInfo;
            il.Emit(opcode, fieldInfo);
        }

        public static void Emit(this ILGenerator il, OpCode opcode, IStaticFieldElement el) {
            var m = el as IMemberInfoSource;
            var fieldInfo = m.MemberInfo as FieldInfo;
            il.Emit(opcode, fieldInfo);
        }

        public static void Emit(this ILGenerator il, OpCode opcode, IMethodElement el) {
            var m = el as IMemberInfoSource;
            var methodInfo = m.MemberInfo as MethodInfo;
            il.Emit(opcode, methodInfo);
        }

        public static void Emit(this ILGenerator il, OpCode opcode, IEventElement el) {
            var m = el as IMemberInfoSource;
            var methodInfo = m.MemberInfo as MethodInfo;
            il.Emit(opcode, methodInfo);
        }

        public interface IMemberInfoSource
        {
            object MemberInfo { get; }
        }

        #endregion



        public static Type Design(Action<ITypeDesigner> fnDesign) {
            var designer = new TypeDesigner();
            fnDesign(designer);
            return designer.CreateType();
        }



        #region Element Interfaces


        public interface IFieldElement
        {
            IFieldElement MakePublic();
        }


        public interface IStaticFieldElement
        {
            IStaticFieldElement Value(object value);
        }


        public interface IPropertyElement
        {
            IPropertyElement ParamTypes(Type[] types);
            IPropertyElement EmitGet(Action<ILGenerator> fn);
            IPropertyElement EmitSet(Action<ILGenerator> fn);
            IPropertyElement UseBackingField(IFieldElement backingField);
            IPropertyElement InjectOnChanged(Action<ILGenerator> fn);
        }


        public interface IOverridePropertyElement
        {
            IOverridePropertyElement EmitGet(Action<ILGenerator> fn);
            IOverridePropertyElement EmitSet(Action<ILGenerator> fn);
        }

        public interface IMethodElement
        {
            IMethodElement ReturnType(Type type);
            IMethodElement ArgTypes(params Type[] types);
            IMethodElement Attributes(MethodAttributes att);
            IMethodElement Emit(Action<ILGenerator> fn);
        }

        public interface IOverrideMethodElement
        {
            IOverrideMethodElement Emit(Action<ILGenerator> fn);
        }

        public interface ICtorElement
        {
            ICtorElement ArgTypes(params Type[] types);
            ICtorElement Emit(Action<ILGenerator> fn);
            ICtorElement EmitAction(Action<ILGenerator> fn);
            ICtorElement PassThroughToBaseCtor();
        }

        public interface IEventElement
        {
            IEventElement EmitAdd(Action<ILGenerator> fn);
            IEventElement EmitRemove(Action<ILGenerator> fn);
            IEventElement EventArgsType(Type type);
            IEventElement EventArgsType<T>() where T : class;
            IEventElement PrivateField();
        }

        #endregion



        public interface ITypeDesigner
        {
            string Name { get; set; }
            Type BaseType { get; set; }
            TypeAttributes Attributes { get; set; }

            void AddInterface(params Type[] rtInt);

            IFieldElement Field(string name, Type type);
            IStaticFieldElement StaticField(string name, Type type);

            IPropertyElement Property(string name, Type type);
            IOverridePropertyElement OverrideProperty(PropertyInfo info);
            IOverridePropertyElement OverrideProperty<T>(Expression<Func<T, object>> exp);

            IMethodElement Method(string name);
            IMethodElement OverrideMethod(MethodInfo info);
            IMethodElement OverrideMethod<T>(Expression<Action<T>> exp);

            ICtorElement Constructor();

            IEventElement Event(string name);
            IEventElement OverrideEvent(EventInfo info);
        }



        class TypeDesigner : ITypeDesigner
        {
            string _name = $"DynaType_{Guid.NewGuid()}";
            Type _baseType = null;
            List<Type> _lInterfaces = new List<Type>();
            TypeAttributes _attributes = TypeAttributes.BeforeFieldInit | TypeAttributes.Public;

            List<DesignElement> _lDesignElements = new List<DesignElement>();
                        

            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }


            public Type BaseType
            {
                get { return _baseType; }
                set { _baseType = value; }
            }

            public TypeAttributes Attributes
            {
                get { return _attributes; }
                set { _attributes = value; }
            }


            public void AddInterface(params Type[] rtInt) {
                _lInterfaces.AddRange(rtInt);
            }


            //----------------------------------------------------------------


            public abstract class DesignElement
            {
                public abstract void PreDefine(ITypeDesigner t);
                public abstract void Define(TypeBuilder bl);
                public abstract void PostDefine();
                public abstract void PostCreate(Type type);
            }


            #region Fields

            class FieldElement : DesignElement, IFieldElement, IMemberInfoSource
            {
                string _name;
                Type _type;
                FieldAttributes _attributes = FieldAttributes.Private;

                FieldBuilder _blf;

                public FieldElement(string name, Type type) {
                    _name = name;
                    _type = type;
                }


                public override void PreDefine(ITypeDesigner t) {
                    //...
                }

                public override void Define(TypeBuilder bl) {
                    _blf = bl.DefineField(_name, _type, _attributes);
                }

                public override void PostDefine() {
                    //...
                }

                public override void PostCreate(Type type) {
                    //...
                }


                IFieldElement IFieldElement.MakePublic() {
                    _attributes = FieldAttributes.Public;
                    return this;
                }


                object IMemberInfoSource.MemberInfo
                {
                    get { return _blf; }
                }
            }


            public IFieldElement Field(string name, Type type) {
                var el = new FieldElement(name, type);
                _lDesignElements.Add(el);
                return el;
            }



            #endregion


            #region Static Fields

            class StaticFieldElement : DesignElement, IStaticFieldElement, IMemberInfoSource
            {
                string _name;
                Type _type;
                object _value;
                FieldAttributes _attributes = FieldAttributes.Public | FieldAttributes.Static;

                FieldBuilder _blf;


                public StaticFieldElement(string name, Type type) {
                    _name = name;
                    _type = type;
                }


                public override void PreDefine(ITypeDesigner t) {
                    //...
                }

                public override void Define(TypeBuilder bl) {
                    _blf = bl.DefineField(_name, _type, _attributes);
                }

                public override void PostDefine() {
                    //...
                }

                public override void PostCreate(Type type) {
                    var field = type.GetField(_name, BindingFlags.Public | BindingFlags.Static);
                    field.SetValue(null, _value);
                }


                IStaticFieldElement IStaticFieldElement.Value(object value) {
                    _value = value;
                    return this;
                }


                //...


                object IMemberInfoSource.MemberInfo
                {
                    get { return _blf; }
                }
            }


            public IStaticFieldElement StaticField(string name, Type type) {
                var el = new StaticFieldElement(name, type);
                _lDesignElements.Add(el);
                return el;
            }


            #endregion


            #region Properties

            class PropertyElement : DesignElement, IPropertyElement
            {
                string _name;
                Type _type;
                Type[] _parameterTypes = Type.EmptyTypes;
                PropertyAttributes _attributes = PropertyAttributes.None;

                Action<ILGenerator> _fnGet = null, _fnSet = null;
                Action<ILGenerator> _fnOnChanged = null;

                IFieldElement _backingField = null;
                MethodBuilder _blmGet = null, _blmSet = null;
                PropertyBuilder _blp = null;

                static MethodAttributes _methodAtts = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

                public PropertyElement(string name, Type type) {
                    _name = name;
                    _type = type;
                }


                public override void PreDefine(ITypeDesigner t) {
                    if(_backingField != null) {
                        _fnGet = (il) => {
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, _backingField);
                            il.Emit(OpCodes.Ret);
                        };

                        _fnSet = (il) => {
                            if(_fnOnChanged != null) {
                                var lbSkipOnChanged = il.DefineLabel();

                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldfld, _backingField);

                                if(_type.IsValueType) {
                                    il.Emit(OpCodes.Box, _type);
                                }

                                il.Emit(OpCodes.Ldarg_1);

                                if(_type.IsValueType) {
                                    il.Emit(OpCodes.Box, _type);
                                }

                                il.Emit(OpCodes.Call, typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static));

                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Stfld, _backingField);

                                il.Emit(OpCodes.Brtrue, lbSkipOnChanged);

                                _fnOnChanged(il);

                                il.MarkLabel(lbSkipOnChanged);
                                il.Emit(OpCodes.Ret);
                            }
                            else {
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Stfld, _backingField);
                                il.Emit(OpCodes.Ret);
                            }
                        };
                    }
                }

                public override void Define(TypeBuilder bl) {
                    _blp = bl.DefineProperty(_name, _attributes, _type, _parameterTypes);

                    if(_fnGet != null) {
                        _blmGet = bl.DefineMethod("get_" + _name, _methodAtts, _type, _parameterTypes);
                        _blp.SetGetMethod(_blmGet);
                    }

                    if(_fnSet != null) {
                        _blmSet = bl.DefineMethod("set_" + _name, _methodAtts, typeof(void), _parameterTypes.Concat(new[] { _type }).ToArray());
                        _blmSet.DefineParameter(_parameterTypes.Length + 1, ParameterAttributes.None, "value"); //this is needed to please locals window, which relies on it secretly
                        _blp.SetSetMethod(_blmSet);
                    }
                }

                public override void PostDefine() {
                    if(_blmGet != null)
                        _fnGet(_blmGet.GetILGenerator());

                    if(_blmSet != null)
                        _fnSet(_blmSet.GetILGenerator());
                }

                public override void PostCreate(Type type) {
                    //...
                }


                IPropertyElement IPropertyElement.EmitGet(Action<ILGenerator> fn) {
                    _fnGet = fn;
                    _backingField = null;
                    return this;
                }

                IPropertyElement IPropertyElement.EmitSet(Action<ILGenerator> fn) {
                    _fnSet = fn;
                    _backingField = null;
                    return this;
                }

                public IPropertyElement ParamTypes(Type[] types) {
                    //...
                    //...
                    return this;
                }

                IPropertyElement IPropertyElement.UseBackingField(IFieldElement backingField) {
                    _backingField = backingField;
                    return this;
                }


                IPropertyElement IPropertyElement.InjectOnChanged(Action<ILGenerator> fn) {
                    _fnOnChanged = fn;
                    return this;
                }


            }


            public IPropertyElement Property(string name, Type type) {
                var el = new PropertyElement(name, type);
                _lDesignElements.Add(el);
                return el;
            }

            #endregion


            #region OverrideProperties

            class OverridePropertyElement : DesignElement, IOverridePropertyElement
            {

                PropertyInfo _info;
                Action<ILGenerator> _fnGet = null, _fnSet = null;

                public OverridePropertyElement(PropertyInfo info) {
                    _info = info;
                }


                public override void PreDefine(ITypeDesigner t) {
                    if(_fnGet != null) {
                        var mtGet = _info.GetGetMethod();

                        if(mtGet == null)
                            throw new Exception("Can't override non-existent get method on property " + _info.Name);

                        t.OverrideMethod(mtGet).Emit(_fnGet);
                    }

                    if(_fnSet != null) {
                        var mtSet = _info.GetSetMethod();

                        if(mtSet == null)
                            throw new Exception("Can't override non-existent set method on property " + _info.Name);

                        t.OverrideMethod(mtSet).Emit(_fnSet);
                    }
                }


                public override void Define(TypeBuilder bl) {
                    //...
                }

                public override void PostDefine() {
                    //...
                }

                public override void PostCreate(Type type) {
                    //...
                }


                IOverridePropertyElement IOverridePropertyElement.EmitGet(Action<ILGenerator> fn) {
                    _fnGet = fn;
                    return this;
                }

                IOverridePropertyElement IOverridePropertyElement.EmitSet(Action<ILGenerator> fn) {
                    _fnSet = fn;
                    return this;
                }

            }


            public IOverridePropertyElement OverrideProperty(PropertyInfo info) {
                var el = new OverridePropertyElement(info);
                _lDesignElements.Add(el);
                return el;
            }

            public IOverridePropertyElement OverrideProperty<T>(Expression<Func<T, object>> expProp) {

                Expression exp = expProp.Body;

                if(exp.NodeType == ExpressionType.Convert) {
                    exp = (exp as UnaryExpression).Operand;
                }

                var expMember = exp as MemberExpression;

                if(expMember != null) {
                    var info = expMember.Member as PropertyInfo;

                    if(info != null) {
                        return OverrideProperty(info);
                    }
                }

                throw new Exception("Bad expression fed to OverrideProperty()!");
            }

            #endregion


            #region Methods

            class MethodElement : DesignElement, IMethodElement, IMemberInfoSource
            {
                string _name;
                Type _returnType = typeof(void);
                Type[] _argTypes = null;
                MethodAttributes _attributes = MethodAttributes.Public | MethodAttributes.Virtual;

                Action<ILGenerator> _fn = null;

                MethodBuilder _blm;

                public MethodElement(string name) {
                    _name = name;
                }

                public override void PreDefine(ITypeDesigner t) {
                    //...
                }

                public override void Define(TypeBuilder bl) {
                    _blm = bl.DefineMethod(_name, _attributes, _returnType, _argTypes);
                }

                public override void PostDefine() {
                    _fn(_blm.GetILGenerator());
                }

                public override void PostCreate(Type type) {
                    //...
                }


                public IMethodElement ReturnType(Type type) {
                    _returnType = type;
                    return this;
                }

                public IMethodElement ArgTypes(params Type[] types) {
                    _argTypes = types;
                    return this;
                }

                public IMethodElement Attributes(MethodAttributes att) {
                    _attributes = att;
                    return this;
                }

                public IMethodElement Emit(Action<ILGenerator> fn) {
                    _fn = fn;
                    return this;
                }


                //...


                object IMemberInfoSource.MemberInfo
                {
                    get { return _blm; }
                }
            }


            public IMethodElement Method(string name) {
                var el = new MethodElement(name);
                _lDesignElements.Add(el);
                return el;
            }


            public IMethodElement OverrideMethod(MethodInfo info) {
                var el = new MethodElement(info.Name)
                                .ArgTypes(info.GetParameters().Select(p => p.ParameterType).ToArray())
                                .ReturnType(info.ReturnType)
                                .Attributes(MethodAttributes.Virtual | MethodAttributes.Public);

                _lDesignElements.Add(el as DesignElement);
                return el;
            }

            public IMethodElement OverrideMethod<T>(Expression<Action<T>> exp) {
                if(exp.Body is MethodCallExpression) {
                    var exCall = (MethodCallExpression)exp.Body;
                    var methodInfo = exCall.Method;

                    if(methodInfo.DeclaringType.Equals(typeof(T))) {
                        var el = new MethodElement(methodInfo.Name)
                                        .ArgTypes(methodInfo.GetParameters().Select(p => p.ParameterType).ToArray())
                                        .ReturnType(methodInfo.ReturnType)
                                        .Attributes(MethodAttributes.Virtual | MethodAttributes.Public);

                        _lDesignElements.Add(el as DesignElement);
                        return el;
                    }
                }

                throw new ArgumentException("Bad expression passed to OverrideMethod!");
            }


            #endregion


            #region Constructors

            class CtorElement : DesignElement, ICtorElement
            {

                MethodAttributes _attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                Type[] _argTypes;
                Action<ILGenerator> _fn = null;
                Queue<Action<ILGenerator>> _qFnActions = new Queue<Action<ILGenerator>>();
                bool _passToBase = false;

                TypeBuilder _bl;
                ConstructorBuilder _blc;

                public override void PreDefine(ITypeDesigner t) {
                    //...
                }

                public override void Define(TypeBuilder bl) {
                    _bl = bl;
                    _blc = bl.DefineConstructor(_attributes, CallingConventions.HasThis, _argTypes);
                }

                public override void PostDefine() {
                    int i = 0;
                    var il = _blc.GetILGenerator();

                    //each action to have its own private function
                    while(_qFnActions.Count > 0) {
                        var fnAction = _qFnActions.Dequeue();

                        var mtAction = _bl.DefineMethod("_CtorAction" + (i++), MethodAttributes.Private, null, _argTypes);
                        fnAction(mtAction.GetILGenerator());

                        //now emit code in ctor proper to call action
                        //load all args onto stack then invoke - that's it!
                        for(int iArg = 0; iArg < _argTypes.Length + 1; iArg++) {
                            il.Emit(OpCodes.Ldarg, iArg);
                        }

                        il.Emit(OpCodes.Call, mtAction);
                    }

                    if(_fn != null) {
                        _fn.Invoke(il);
                    }
                    else {
                        if(_passToBase) {
                            if(_bl.BaseType == null) {
                                throw new ArgumentException("Can't pass through to base ctor if no base specified!");
                            }

                            var baseCtor = _bl.BaseType.GetConstructor(_argTypes);

                            if(baseCtor == null) {
                                throw new ArgumentException("Can't find an appropriate base ctor to pass to!");
                            }

                            for(int iArg = 0; iArg < _argTypes.Length + 1; iArg++) {
                                il.Emit(OpCodes.Ldarg, iArg);
                            }

                            il.Emit(OpCodes.Call, baseCtor);
                        }

                        il.Emit(OpCodes.Ret);
                    }
                }

                public override void PostCreate(Type type) {
                    //...
                }


                ICtorElement ICtorElement.ArgTypes(params Type[] types) {
                    _argTypes = types;
                    return this;
                }

                ICtorElement ICtorElement.Emit(Action<ILGenerator> fn) {
                    _fn = fn;
                    return this;
                }

                ICtorElement ICtorElement.EmitAction(Action<ILGenerator> fn) {
                    _qFnActions.Enqueue(fn);
                    return this;
                }

                ICtorElement ICtorElement.PassThroughToBaseCtor() {
                    _passToBase = true;
                    return this;
                }
            }


            public ICtorElement Constructor() {
                var el = new CtorElement();
                _lDesignElements.Add(el);
                return el;
            }



            #endregion


            #region Events

            class EventElement : DesignElement, IEventElement, IMemberInfoSource
            {
                string _name;
                Type _handlerType = null;
                Type _eventArgsType = typeof(EventArgs);
                EventAttributes _attributes = EventAttributes.None;

                Action<ILGenerator> _fnAdd = null, _fnRemove = null;
                EventBuilder _ble;
                MethodBuilder _blmAdd, _blmRemove, _blmInvoke;

                bool _bPrivateField = true;
                IFieldElement _field;

                MethodAttributes _methodAtts =
                                      MethodAttributes.Public |
                                      MethodAttributes.SpecialName |
                                      MethodAttributes.NewSlot |
                                      MethodAttributes.HideBySig |
                                      MethodAttributes.Virtual |
                                      MethodAttributes.Final;

                public EventElement(string name) {
                    _name = name;
                }

                public EventElement(EventInfo info) {
                    _name = info.Name;
                    _attributes = info.Attributes;
                    _handlerType = info.EventHandlerType;
                    _eventArgsType = _handlerType.GetMethod("Invoke").GetParameters()[1].ParameterType;
                    _bPrivateField = true;
                }


                public override void PreDefine(ITypeDesigner t) {
                    if(_handlerType == null) {
                        _handlerType = _eventArgsType == typeof(EventArgs)
                                                    ? typeof(EventHandler)
                                                    : typeof(EventHandler<>).MakeGenericType(_eventArgsType);
                    }

                    if(_bPrivateField) {
                        _field = t.Field("_" + _name, _handlerType);

                        _fnAdd = (il) => {
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, _field);
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, typeof(Delegate).GetMethod("Combine", new[] { typeof(Delegate), typeof(Delegate) }, null));
                            il.Emit(OpCodes.Castclass, _handlerType);
                            il.Emit(OpCodes.Stfld, _field);
                            il.Emit(OpCodes.Ret);
                        };

                        _fnRemove = (il) => {
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, _field);
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, typeof(Delegate).GetMethod("Remove", new[] { typeof(Delegate), typeof(Delegate) }, null));
                            il.Emit(OpCodes.Castclass, _handlerType);
                            il.Emit(OpCodes.Stfld, _field);
                            il.Emit(OpCodes.Ret);
                        };
                    }
                }

                public override void Define(TypeBuilder bl) {
                    _ble = bl.DefineEvent(_name, _attributes, _handlerType);

                    if(_fnAdd != null) {
                        _blmAdd = bl.DefineMethod("add_" + _name, _methodAtts, null, new[] { _handlerType });
                        _blmAdd.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);
                        _ble.SetAddOnMethod(_blmAdd);
                    }

                    if(_fnRemove != null) {
                        _blmRemove = bl.DefineMethod("remove_" + _name, _methodAtts, null, new[] { _handlerType });
                        _blmRemove.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);
                        _ble.SetRemoveOnMethod(_blmRemove);
                    }

                    if(_bPrivateField) {
                        _blmInvoke = bl.DefineMethod("On" + _name, MethodAttributes.Private, null, new[] { typeof(object), _eventArgsType });
                    }
                }

                public override void PostDefine() {
                    if(_blmAdd != null)
                        _fnAdd(_blmAdd.GetILGenerator());

                    if(_blmRemove != null)
                        _fnRemove(_blmRemove.GetILGenerator());

                    if(_blmInvoke != null) {
                        var il = _blmInvoke.GetILGenerator();
                        var lbExit = il.DefineLabel();

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, _field);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brtrue, lbExit);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, _field);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Callvirt, _handlerType.GetMethod("Invoke"));

                        il.MarkLabel(lbExit);
                        il.Emit(OpCodes.Ret);
                    }
                }

                public override void PostCreate(Type type) {
                    //...
                }


                public IEventElement EmitAdd(Action<ILGenerator> fn) {
                    _fnAdd = fn;
                    return this;
                }

                public IEventElement EmitRemove(Action<ILGenerator> fn) {
                    _fnRemove = fn;
                    return this;
                }

                public IEventElement EventArgsType(Type type) {
                    _eventArgsType = type;
                    return this;
                }

                public IEventElement EventArgsType<T>() where T : class {
                    return EventArgsType(typeof(T));
                }

                public IEventElement PrivateField() {
                    _bPrivateField = true;
                    return this;
                }


                object IMemberInfoSource.MemberInfo
                {
                    get { return _blmInvoke; }
                }

            }


            public IEventElement Event(string name) {
                var el = new EventElement(name);
                _lDesignElements.Add(el);
                return el;
            }

            public IEventElement OverrideEvent(EventInfo info) {
                var el = new EventElement(info);
                _lDesignElements.Add(el);
                return el;
            }


            #endregion



            //----------------------------------------


            public Type CreateType() {
                var bl = _blMod.DefineType(_name, _attributes, _baseType, _lInterfaces.ToArray());

                for(int i = 0; i < _lDesignElements.Count; i++) { //such iteration needed to allow additions within enumeration
                    _lDesignElements[i].PreDefine(this);
                }

                foreach(var element in _lDesignElements)
                    element.Define(bl);

                foreach(var element in _lDesignElements)
                    element.PostDefine();

                var type = bl.CreateType();

                foreach(var element in _lDesignElements)
                    element.PostCreate(type);
                
                return type;
            }

        }


    }



}