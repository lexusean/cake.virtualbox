using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core;
using Cake.Core.Configuration;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Tooling;
using Cake.Testing;

namespace Cake.Virtualbox.Test.System
{
    //IFileSystem is FileSystem -
    //ICakeEnvironment is CakeEnvironment -
    //ICakePlatform is CakePlatform -
    //ICakeRuntime is CakeRuntime -
    //ICakeLog is FakeCakeLog -
    //ProcessRunner is ProcessRunner - 
    //IToolLocator is ToolLocator -
    //IToolRepository is ToolRepository -
    //IToolResolutionStrategy is To ToolResolutionStrategy - 
    //IGlobber is Globber -
    //ICakeConfiguration is From CakeConfigurationProvider -
    // get from provider.CreateConfiguration(CakeOptions.Script.GetDirectory(), CakeOptions.Arguments)
    //CakeOptions 

    public static class CakeFixtures
    {
        private static Lazy<ICakeLog> _CkLog = new Lazy<ICakeLog>(() => new FakeLog());
        public static ICakeLog CkLog
        {
            get { return _CkLog.Value; }
        }

        private static Lazy<IFileSystem> _CkFileSystem = new Lazy<IFileSystem>(() => new FileSystem());
        public static IFileSystem CkFileSystem
        {
            get { return _CkFileSystem.Value; }
        }

        private static Lazy<ICakePlatform> _CkPlatform = new Lazy<ICakePlatform>(() => new CakePlatform());
        public static ICakePlatform CkPlatform
        {
            get { return _CkPlatform.Value; }
        }

        private static Lazy<ICakeRuntime> _CkRuntime = new Lazy<ICakeRuntime>(() => new CakeRuntime());
        public static ICakeRuntime CkRuntime
        {
            get { return _CkRuntime.Value; }
        }

        private static Lazy<ICakeEnvironment> _CkEnv = new Lazy<ICakeEnvironment>(() => new CakeEnvironment(CkPlatform, CkRuntime, CkLog));
        public static ICakeEnvironment CkEnv
        {
            get { return _CkEnv.Value; }
        }

        private static Lazy<IProcessRunner> _CkProcRunner = new Lazy<IProcessRunner>(() => new ProcessRunner(CkEnv, CkLog));
        public static IProcessRunner CkProcRunner
        {
            get { return _CkProcRunner.Value; }
        }

        private static Lazy<IToolRepository> _CkToolRepo = new Lazy<IToolRepository>(() => new ToolRepository(CkEnv));
        public static IToolRepository CkToolRepo
        {
            get { return _CkToolRepo.Value; }
        }

        private static Lazy<IGlobber> _CkGlobber = new Lazy<IGlobber>(() => new Globber(CkFileSystem, CkEnv));
        public static IGlobber CkGlobber
        {
            get { return _CkGlobber.Value; }
        }

        private static Lazy<CakeOptionsFixture> _CkOptions = new Lazy<CakeOptionsFixture>(CakeOptionsFixture.CreateOptions);
        public static CakeOptionsFixture CkOptions
        {
            get { return _CkOptions.Value; }
        }

        private static Lazy<ICakeConfiguration> _CkConfig = new Lazy<ICakeConfiguration>(() =>
        {
            var provider = new CakeConfigurationProvider(CkFileSystem, CkEnv);
            return provider.CreateConfiguration(CkOptions.Script.GetDirectory(), CkOptions.Arguments);
        });
        public static ICakeConfiguration CkConfig
        {
            get { return _CkConfig.Value; }
        }

        private static Lazy<IToolResolutionStrategy> _CkToolResStrategy = new Lazy<IToolResolutionStrategy>(() => new ToolResolutionStrategy(CkFileSystem, CkEnv, CkGlobber, CkConfig));
        public static IToolResolutionStrategy CkToolResStrategy
        {
            get { return _CkToolResStrategy.Value; }
        }

        private static Lazy<IToolLocator> _CkToolLocator = new Lazy<IToolLocator>(() => new ToolLocator(CkEnv, CkToolRepo, CkToolResStrategy));
        public static IToolLocator CkToolLocator
        {
            get { return _CkToolLocator.Value; }
        }
    }

}
