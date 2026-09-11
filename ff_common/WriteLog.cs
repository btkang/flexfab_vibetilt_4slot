using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace ff_common
{
    public class WriteLog
    {
        public static void SaveLog(string serial, string mac, Dictionary<string, object> testResults)
        {
            // 현재 시간을 기준으로 타임스탬프 생성
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 로그 데이터 구조 생성
            var logData = new
            {
                serial_no = serial,
                mac_address = mac,
                timestamp = timestamp,
                result = testResults
            };

            // 파일 저장 경로 설정
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath); // 폴더가 없으면 생성
            }

            string filePath = Path.Combine(directoryPath, $"{serial}.json");

            // 파일에 JSON 형식으로 저장
            try
            {
                string json = JsonConvert.SerializeObject(logData, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그 저장 중 오류 발생: {ex.Message}");
            }
        }
    }
}
