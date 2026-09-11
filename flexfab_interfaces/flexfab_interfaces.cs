using System.Dynamic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;



namespace Cantops.FlexFab
{
    /// <summary>
    /// 로그 레벨 설정. "TEST_LOG" = 전체 출력, "MP_LOG" = 핵심만 출력.
    /// </summary>
    public static class LogConfig
    {
        public static string LogLevel = "MP_LOG";
        public static bool IsTest => LogLevel.Equals("TEST_LOG", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 기본 인터페이스로, 모든 공통 메서드를 정의합니다.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Returns information about the class.
        /// </summary>
        IDictionary<string, object?> GetInfo();

        /// <summary>
        /// Sets configuration for the object.
        /// </summary>
        void SetConfig(IDictionary<string, object?> config);

        /// <summary>
        /// Retrieves the current status of the object.
        /// </summary>
        IDictionary<string, object?>? GetStatus();

        void PreRun(IDictionary<string, object?> libraries);

        void PostRun();
    }

    /// <summary>
    /// IModule의 기본 구현체.
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        protected IDictionary<string, object?>? info_ = null;
        protected IDictionary<string, object?>? status_ = null;
        protected Action<string>? log_action_ = null;
        protected IDictionary<string, object?>? config_ = null;
        protected IDictionary<string, object?>? libraries_ = null;

        private void CheckIinfo()
        {
            if (info_ == null)
            {
                throw new InvalidOperationException("info_ must be initialized in constructor");
            }
            if (info_.ContainsKey("name") == false)
            {
                throw new InvalidOperationException("name of info_ must be defined");
            }
        }

        public IDictionary<string, object?> GetInfo()
        {
            if (info_ == null)
            {
                throw new InvalidOperationException("info_ must be initialized in constructor");
            }
            return info_;
        }

        /*public IDictionary<string, object?>? GetStatus()
        {
            return status_;
        }*/

        public virtual IDictionary<string, object?>? GetStatus()
        {
            // 기본 구현을 반환 (필요에 따라 수정 가능)
            return null;
        }

        public virtual void SetConfig(IDictionary<string, object?> config)
        {
            config_ = config;
            if (config.TryGetValue("log_action", out var value) && value is Action<string> logact)
            {
                log_action_ = logact;
            }
        }

        public void Log(string message)
        {
            CheckIinfo();
            if (log_action_ == null) return;
            log_action_($"{info_?["name"]} {message}");
        }

        public virtual void PreRun(IDictionary<string, object?> libraries)
        {
            libraries_ = libraries;
        }

        public virtual void PostRun()
        {
            libraries_ = null;
        }
    }

    /// <summary>
    /// 실행 관련 메서드를 정의하는 인터페이스.
    /// </summary>
    public interface IProcess : IModule
    {
        /// <summary>
        /// Starts the execution process.
        /// </summary>
        void Begin();

        /// <summary>
        /// Ends the execution process.
        /// </summary>
        void End();
    }

    /// <summary>
    /// 통신 관련 메서드를 정의하는 인터페이스.
    /// </summary>
    public interface IComm : IModule
    {
        /// <summary>
        /// Establishes a connection.
        /// </summary>
        bool Connect();

        /// <summary>
        /// Disconnects the current connection.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sets the communication timeout in milliseconds.
        /// </summary>
        void SetTimeout(int timeout);

        /// <summary>
        /// Sends a message.
        /// </summary>
        void Send(IDictionary<string, object?> param);

        /// <summary>
        /// Receives a message with optional parameters.
        /// </summary>
        IDictionary<string, object?>? Recv(IDictionary<string, object?>? param);
    }

    public interface IProtocol : IModule
    {
        IDictionary<string, object?>? DoCommand(IDictionary<string, object?> command);
    }

    /// <summary>
    /// 장치 관련 메서드를 정의하는 인터페이스.
    /// </summary>
    public interface IDevice : IModule
    {
        /// <summary>
        /// Initializes the device.
        /// </summary>
        void Initialize(IDictionary<string, object?> libraries);

        /// <summary>
        /// Sends data to the device.
        /// </summary>
        void Write(IDictionary<string, object?> param);

        /// <summary>
        /// Reads data from the device.
        /// </summary>
        IDictionary<string, object?> Read(IDictionary<string, object?> param);
    }

    /// <summary>
    /// 예시로 기존의 IProcess 및 IComm 인터페이스들을 B 방식에 맞게 재구성한 예시.
    /// </summary>
    public class Process : IProcess
    {
        private IDictionary<string, object?>? config_;
        private Action<string>? log_action_;

        public IDictionary<string, object?> GetInfo()
        {
            return new Dictionary<string, object?> { { "name", "Example Process" } };
        }

        public void SetConfig(IDictionary<string, object?> config)
        {
            config_ = config;

            // 'log_action' 키가 존재하는지 확인하고, 있으면 그 값을 가져옵니다.
            if (config?.ContainsKey("log_action") == true)
            {
                log_action_ = config["log_action"] as Action<string>;
            }
        }

        public IDictionary<string, object?>? GetStatus()
        {
            return new Dictionary<string, object?> { { "status", "Ready" } };
        }

        public void PreRun(IDictionary<string, object?> libraries)
        {
            // 필요에 따라 libraries를 활용한 초기화
        }

        public void PostRun()
        {
            // 실행 후 처리
        }

        public void Begin()
        {
            log_action_?.Invoke("Process Started");
        }

        public void End()
        {
            log_action_?.Invoke("Process Ended");
        }
    }
}