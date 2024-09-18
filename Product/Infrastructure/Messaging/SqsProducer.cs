using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    public class SqsProducer
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;
        private readonly ILogger<SqsProducer> _logger;

        public SqsProducer(IAmazonSQS sqsClient, IConfiguration configuration, ILogger<SqsProducer> logger)
        {
            _sqsClient = sqsClient;
            _queueUrl = configuration.GetValue<string>("AWS:SqsQueueUrl");
            _logger = logger;
        }

        /// <summary>
        /// Envia uma mensagem para a fila SQS.
        /// </summary>
        /// <param name="messageBody">Produto que foi criado.</param>
        /// <returns>True se a mensagem foi enviada com sucesso, False se houve algum erro.</returns>
        public async Task<bool> SendMessageAsync(string messageBody)
        {
            try
            {
                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = messageBody
                };

                var response = await _sqsClient.SendMessageAsync(sendMessageRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Mensagem enviada com sucesso para a fila SQS. ID da mensagem: {response.MessageId}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Falha ao enviar mensagem para a fila SQS. Status Code: {response.HttpStatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem para a fila SQS.");
                return false; 
            }
        }
    }
}
