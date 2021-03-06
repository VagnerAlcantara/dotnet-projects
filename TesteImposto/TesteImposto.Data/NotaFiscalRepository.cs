﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using TesteImposto.Domain;
using TesteImposto.Domain.Interfaces.Repositories;
using TesteImposto.Shared;
using TesteImposto.Shared.Xml;

namespace TesteImposto.Data
{
    public class NotaFiscalRepository : Notification, INotaFiscalRepository
    {
        /// <summary>
        /// Grava a nota fiscal
        /// </summary>
        /// <param name="notaFiscal">Dados da Nota fiscal</param>
        public void GravarNotaFiscal(NotaFiscal notaFiscal)
        {
            try
            {
                using (var scope = new TransactionScope())
                {
                    notaFiscal.Id = Gravar(notaFiscal);
                    NotaFiscalItemRepository notaFiscalItemRepository = new NotaFiscalItemRepository();

                    notaFiscalItemRepository.GravarItemNotaFiscal(notaFiscal.ItensDaNotaFiscal, notaFiscal.Id);

                    bool xmlGerado = GerarXml(notaFiscal);

                    if (xmlGerado)
                        scope.Complete();
                }
            }
            catch (Exception ex)
            {
                AddError(ex.Message);
            }
        }

        /// <summary>
        /// Grava a nota fiscal
        /// </summary>
        /// <param name="notaFiscal">Dados da Nota fiscal</param>
        private int Gravar(NotaFiscal notaFiscal)
        {
            int idNotaFiscal = 0;

            List<SqlParameter> parameterList = new List<SqlParameter>()
            {
                new SqlParameter() { ParameterName = "@pId", Value = notaFiscal.Id, Direction = ParameterDirection.InputOutput, SqlDbType = SqlDbType.Int },
                new SqlParameter() { ParameterName = "@pNumeroNotaFiscal", Value = notaFiscal.NumeroNotaFiscal, SqlDbType = SqlDbType.Int },
                new SqlParameter() { ParameterName = "@pSerie", Value = notaFiscal.Serie, SqlDbType = SqlDbType.Int },
                new SqlParameter() { ParameterName = "@pNomeCliente", Value = notaFiscal.NomeCliente, SqlDbType = SqlDbType.VarChar, Size = 50 },
                new SqlParameter() { ParameterName = "@pEstadoDestino", Value = notaFiscal.EstadoDestino, SqlDbType = SqlDbType.VarChar, Size = 50 },
                new SqlParameter() { ParameterName = "@pEstadoOrigem", Value = notaFiscal.EstadoOrigem, SqlDbType = SqlDbType.VarChar, Size = 50 }
            };

            using (var conn = new SqlConnection(DbContext.SqlConn))
            {
                conn.Open();
                using (SqlCommand sqlCommand = new SqlCommand("P_NOTA_FISCAL", conn))
                {
                    try
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.Parameters.AddRange(parameterList.ToArray());
                        var result = sqlCommand.ExecuteNonQuery();

                        if (result == 0)
                            throw new Exception("Erro ao gravar Nota Fiscal.");

                        idNotaFiscal = (int)sqlCommand.Parameters["@pId"].Value;
                    }
                    catch (Exception ex)
                    {
                        AddError("Erro ao gravar Nota Fiscal. details: " + ex.Message);
                    }
                }
            }
            return idNotaFiscal;
        }

        /// <summary>
        /// Gera o arquivo Xml com dados da nota fiscal num diretório parametrizado pela equipe pré-definido
        /// </summary>
        /// <param name="notaFiscal">Dados da nota fiscal para geração do arquivo Xml</param>
        /// <returns></returns>
        private bool GerarXml(NotaFiscal notaFiscal)
        {
            bool sucesso = false;
            try
            {
                XmlShared.CreateXmlFile<NotaFiscal>(notaFiscal);

                sucesso = true;
            }
            catch (Exception)
            {
                AddError("Erro ao gerar XML");
                sucesso = false;
            }

            return sucesso;
        }
    }
}