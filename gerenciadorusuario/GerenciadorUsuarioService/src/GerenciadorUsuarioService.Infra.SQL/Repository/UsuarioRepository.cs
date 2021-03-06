﻿using Dapper;
using GerenciadorUsuarioService.Dominio.Entidades;
using GerenciadorUsuarioService.Dominio.Interfaces;
using GerenciadorUsuarioService.Infra.SQL.Database;
using GerenciadorUsuarioService.Infra.SQL.SqlCommand;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GerenciadorUsuarioService.Infra.SQL.Repository
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly UsuarioSqlCommand _usuarioSqlCommand;
        private LogRepository _logRepository { get; set; }

        public UsuarioRepository()
        {
            _usuarioSqlCommand = new UsuarioSqlCommand();
            _logRepository = new LogRepository();
        }

        public Usuario Adicionar(Usuario usuario, bool gerarLog)
        {
            try
            {
                ValidarRgCpf(usuario.RG, usuario.CPF, null, usuario.Email);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
            {
                try
                {
                    usuario.PrimeironoNome = this.RetornaPrimeiroNome(usuario.Nome);
                    usuario.Sobrenome = this.RetornaSobrenome(usuario.Nome);

                    var resultArvore = cn.Query<Arvore>(new ArvoreSqlCommand().GetCommandSelectByName(usuario.Arvore)).FirstOrDefault();

                    if (resultArvore != null)
                        usuario.IdArvore = resultArvore.Id;
                    else
                        usuario.IdArvore = 0;

                    usuario.IDLDAP = cn.Query<string>(new UsuarioSqlCommand().GetCommandInsert(usuario)).Single<string>();

                    if (string.IsNullOrEmpty(usuario.IDLDAP))
                        throw new Exception("Erro ao criar usuário!");

                    if (gerarLog)
                        GerarLog("Usuário criado", Convert.ToInt32(usuario.IDLDAP), usuario.Nome, usuario.RG, usuario.CPF);

                    return Buscar(usuario.IDLDAP);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Concat("Erro ao criar usuário: ", ex.Message));
                }
            }
        }

        private void ValidarRgCpf(string rg, string cpf, string idLdap, string email)
        {
            using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
            {
                try
                {
                    cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandValidaRgCpf(rg, cpf, idLdap, email)).ToList();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Concat("Erro ao validar RG e Cpf: ", ex.Message));
                }
            }
        }

        public Usuario Atualizar(Usuario usuario, bool gerarLog)
        {
            try
            {
                ValidarRgCpf(usuario.RG, usuario.CPF, usuario.IDLDAP, usuario.Email);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
            {
                try
                {
                    Usuario oldUsuario = new Usuario();
                    if (gerarLog)
                        oldUsuario = cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectByIdLdap(usuario.IDLDAP)).FirstOrDefault();

                    var resultArvore = cn.Query<Arvore>(new ArvoreSqlCommand().GetCommandSelectByName(usuario.Arvore)).FirstOrDefault();

                    if (resultArvore != null)
                        usuario.IdArvore = resultArvore.Id;
                    else
                        usuario.IdArvore = 0;

                    cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandUpdate(usuario));

                    if (gerarLog)
                        GerarLog("Usuário alterado", Convert.ToInt32(usuario.IDLDAP), usuario.Nome, usuario.RG, usuario.CPF, oldUsuario, usuario);

                    return cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectByIdLdap(usuario.IDLDAP)).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Concat("Erro ao atualizar usuário: ", ex.Message));
                }
            }
        }

        public Usuario AtualizarComSenha(Usuario usuario, bool gerarLog)
        {
            throw new NotImplementedException();
        }

        public void AtualizarSenha(string idLdap, string senha, bool gerarLog)
        {
            using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
            {
                try
                {
                    var usuario = cn.Query<Dominio.Entidades.Usuario>(new UsuarioSqlCommand().GetCommandSelectByIdLdap(idLdap)).FirstOrDefault();

                    if (usuario == null || string.IsNullOrEmpty(usuario.IDLDAP))
                        throw new Exception(string.Concat("Usuário não encontrado ao atualizar senha"));

                    cn.Query<string>(new UsuarioSqlCommand().GetCommandAtualizarSenha(usuario.IDLDAP, senha));

                    if (gerarLog)
                        GerarLog("Senha alterado", Convert.ToInt32(usuario.IDLDAP), usuario.Nome, usuario.RG, usuario.CPF);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Concat("Erro ao atualizar senha: ", ex.Message));
                }
            }
        }



        public void Remover(string idLdap, bool gerarLog)
        {
            throw new NotImplementedException();
        }

        public void RestaurarSenha(string idLdap, bool gerarLog)
        {
            using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
            {
                try
                {
                    var usuario = cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectByIdLdap(idLdap)).FirstOrDefault();

                    cn.Query<string>(new UsuarioSqlCommand().GetCommandResetarSenha(usuario.IDLDAP, usuario.CPF));

                    if (gerarLog)
                        GerarLog("Senha resetada", Convert.ToInt32(usuario.IDLDAP), usuario.Nome, usuario.RG, usuario.CPF);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Concat("Erro ao restaurar senha: ", ex.Message));
                }
            }
        }

        public Usuario Login(Usuario filtro)
        {
            try
            {
                using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
                {
                    if (filtro.IdArvore == 0)
                        filtro.IdArvore = null;

                    Usuario usuario = cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectFiltroWithPassword(filtro)).FirstOrDefault();

                    if (usuario != null)
                    {
                        if (!(string.Equals(usuario.Senha, filtro.Senha)))
                            throw new Exception("Ops! Nossas aplicações foram aperfeiçoadas e, em função dessa manutenção, sua senha foi redefinida automaticamente para o seu CPF (onze dígitos, sem pontos e sem hífen). Tente novamente ou, se preferir, <a href=\"http://www.rededosaber.sp.gov.br/RecuperarSenha/Acoes/LembrarSenha.aspx\" target=\"_blank\">clique aqui</a> aqui para cadastrar uma nova senha pessoal!.");
                    }
                    else
                        throw new Exception("Usuário não encontrado");

                    return usuario;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Usuario LoginByCpf(string cpf, string password)
        {
            try
            {
                using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
                {
                    Usuario usuario = cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectByCpfWithPassword(cpf)).FirstOrDefault();


                    if (usuario == null)
                        throw new Exception("Usuário não encontrado");

                    if (!(string.Equals(usuario.Senha, password)))
                        throw new Exception("Ops! Nossas aplicações foram aperfeiçoadas e, em função dessa manutenção, sua senha foi redefinida automaticamente para o seu CPF (onze dígitos, sem pontos e sem hífen). Tente novamente ou, se preferir, <a href=\"http://www.rededosaber.sp.gov.br/RecuperarSenha/Acoes/LembrarSenha.aspx\" target=\"_blank\">clique aqui</a> aqui para cadastrar uma nova senha pessoal!.");

                    return usuario;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Usuario LoginByName(string name, string password)
        {
            try
            {
                using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
                {
                    Usuario usuario = cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectByNameWithPassword(name)).FirstOrDefault();

                    if (usuario == null)
                        throw new Exception("Usuário não encontrado");

                    if (!(string.Equals(usuario.Senha, password)))
                        throw new Exception("Ops! Nossas aplicações foram aperfeiçoadas e, em função dessa manutenção, sua senha foi redefinida automaticamente para o seu CPF (onze dígitos, sem pontos e sem hífen). Tente novamente ou, se preferir, <a href=\"http://www.rededosaber.sp.gov.br/RecuperarSenha/Acoes/LembrarSenha.aspx\" target=\"_blank\">clique aqui</a> aqui para cadastrar uma nova senha pessoal!.");

                    return usuario;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }



        #region| Buscas
        public ICollection<Usuario> Buscar(Usuario filtro, bool todasPropriedades)
        {
            throw new NotImplementedException();
        }
        public ICollection<Usuario> Buscar(Usuario filtro, Op operador)
        {

            try
            {
                using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
                {
                    var resultArvore = cn.Query<Arvore>(new ArvoreSqlCommand().GetCommandSelectByName(filtro.Arvore)).FirstOrDefault();

                    if (resultArvore != null)
                        filtro.IdArvore = resultArvore.Id;
                    else
                        filtro.IdArvore = null;

                    return cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectFiltro(filtro, operador)).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public ICollection<Usuario> Buscar(ICollection<string> idsLdap, bool todasPropriedades)
        {
            try
            {
                List<Usuario> lstUsuarios = new List<Usuario>();

                using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
                {
                    foreach (string idLdap in idsLdap)
                    {
                        var result = cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectByIdLdap(idLdap)).FirstOrDefault();
                        if (result == null)
                            lstUsuarios.Add(new Usuario() { IDLDAP = idLdap });
                        else
                            lstUsuarios.Add(result);
                    }

                }

                return lstUsuarios;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar usuário: " + ex.Message);
            }
        }
        public ICollection<Usuario> Buscar(ICollection<string> cpfs)
        {
            try
            {
                List<Usuario> lstUsuarios = new List<Usuario>();

                using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
                {
                    foreach (string cpf in cpfs)
                    {

                        var result = cn.Query<Dominio.Entidades.Usuario>(new UsuarioSqlCommand().GetCommandSelectByCpf(cpf)).FirstOrDefault();

                        if (result == null)
                            lstUsuarios.Add(new Usuario() { CPF = cpf });
                        else
                            lstUsuarios.Add(result);
                    }

                }

                return lstUsuarios;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar usuário: " + ex.Message);
            }
        }
        public Usuario Buscar(string idLdap)
        {
            try
            {
                using (var cn = SqlDatabaseConnection.GetOpenConnectionPecReceptor())
                {
                    return cn.Query<Usuario>(new UsuarioSqlCommand().GetCommandSelectByIdLdap(idLdap)).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar usuário: " + ex.Message);
            }
        }
        #endregion

        /// <summary>
        /// A partir de um CN, define GIVENNAME 
        /// </summary>
        /// <param name="valor">String</param>
        /// <returns>String</returns>
        public string RetornaPrimeiroNome(string valor)
        {
            string nome = "";

            for (int i = 0; i < valor.Trim().Length; i++)
            {
                if (valor.Trim().Substring(i, 1) == " ")
                    break;
                else
                    nome += valor.Trim().Substring(i, 1);
            }

            if (nome.Trim() == "")
                return valor;
            else
                return nome.Trim();

        }

        /// <summary>
        /// A partir de um CN, define SN
        /// </summary>
        /// <param name="valor">String</param>
        /// <returns>String</returns>
        public string RetornaSobrenome(string valor)
        {
            string nome = RetornaPrimeiroNome(valor);
            string sobrenome = "";

            int tamanho_nome = (nome.Trim().Length + 1);
            int tamanho_campo = valor.Trim().Length;

            if (tamanho_campo <= tamanho_nome)
                sobrenome = "";
            else
                sobrenome = valor.Trim().Substring(tamanho_nome, (valor.Trim().Length - tamanho_nome));

            if (sobrenome == "")
                return ".";
            else
                return sobrenome.Trim();
        }

        #region| Log - tentar isolar processo

        private void GerarLog(string acao, int? idUsuarioAlterado, string nomeUsuarioAlterado, string rgUsuarioAlterado, string cpfUsuarioAlterado)
        {
            try
            {
                _logRepository = new LogRepository();

                var Acao = _logRepository.ObterTodosAcao().Where(x => x.Nome == acao).FirstOrDefault();

                if (Acao == null)
                    throw new Exception("Erro ao gerar log - Ação não encontrada");

                _logRepository.Adicionar(new Log()
                {
                    DataAcao = DateTime.Now,
                    LogAcaoId = Acao.Id,
                    IdResponsavelAlteracao = Convert.ToInt32(UsuarioLogado.Obter().Id),
                    NomeResponsavelAlteracao = UsuarioLogado.Obter().Nome,
                    IdUsuarioAlterado = idUsuarioAlterado,
                    NomeUsuarioAlterado = nomeUsuarioAlterado,
                    RgUsuarioAlterado = rgUsuarioAlterado,
                    CpfUsuarioAlterado = cpfUsuarioAlterado
                });

            }
            catch (Exception ex)
            {
                throw new Exception(String.Concat("Erro ao gerar log - ", ex.Message));
            }
        }

        private void GerarLog(string acao, int? idUsuarioAlterado, string nomeUsuarioAlterado, string rgUsuarioAlterado, string cpfUsuarioAlterado, Usuario oldUsuario, Usuario newUsuario)
        {
            try
            {

                _logRepository = new LogRepository();

                var Acao = _logRepository.ObterTodosAcao().Where(x => x.Nome == acao).FirstOrDefault();

                if (Acao == null)
                    throw new Exception("Erro ao gerar log - Ação não encontrada");

                _logRepository.Adicionar(new Log()
                {
                    DataAcao = DateTime.Now,
                    LogAcaoId = Acao.Id,
                    IdResponsavelAlteracao = Convert.ToInt32(UsuarioLogado.Obter().Id),
                    NomeResponsavelAlteracao = UsuarioLogado.Obter().Nome,
                    IdUsuarioAlterado = idUsuarioAlterado,
                    NomeUsuarioAlterado = nomeUsuarioAlterado,
                    RgUsuarioAlterado = rgUsuarioAlterado,
                    CpfUsuarioAlterado = cpfUsuarioAlterado,
                    cLogCampos = DePara<Usuario>(oldUsuario, newUsuario)
                });

            }
            catch (Exception ex)
            {
                throw new Exception(String.Concat("Erro ao gerar log - ", ex.Message));
            }
        }

        private ICollection<LogCamposAlterados> DePara<T>(T objetoOld, T objetoNew) where T : class
        {
            try
            {
                if (objetoOld == null || objetoNew == null)
                    throw new Exception("Erro gerar campos para log - não foi possível comparar campos.");

                if (objetoOld.Equals(objetoNew))
                    return null;

                List<LogCamposAlterados> lstCamposDePara = new List<LogCamposAlterados>();


                var props = objetoOld.GetType().GetProperties();

                foreach (var item in props)
                {
                    if ((!typeof(String).Equals(item.PropertyType) && typeof(IEnumerable).IsAssignableFrom(item.PropertyType))) // Se for uma lista
                        continue;

                    if (String.Equals(item.Name.ToLower(), "adspath".ToLower()) ||
                        String.Equals(item.Name.ToLower(), "RGAlteraSenha".ToLower()) ||
                        String.Equals(item.Name.ToLower(), "ValidaCPF".ToLower())
                        )
                        continue;


                    var propValueNew = objetoNew.GetType().GetProperty(item.Name).GetValue(objetoNew, null) == null ? String.Empty : objetoNew.GetType().GetProperty(item.Name).GetValue(objetoNew, null).ToString();

                    var propValueOld = objetoOld.GetType().GetProperty(item.Name).GetValue(objetoOld, null) == null ? String.Empty : objetoOld.GetType().GetProperty(item.Name).GetValue(objetoOld, null).ToString();


                    if (String.Equals(item.Name.ToLower(), "arvore".ToLower()))
                    {
                        propValueNew = propValueNew.Remove(0, propValueNew.IndexOf("ou")).Remove(propValueNew.IndexOf(",o="));
                        propValueOld = propValueOld.Remove(0, propValueOld.IndexOf("ou")).Remove(propValueOld.IndexOf(",o="));
                    }


                    if (propValueNew != propValueOld)
                    {
                        var attribute = item.GetCustomAttributes(typeof(DisplayNameAttribute), true);

                        if (attribute == null ||
                            attribute.Length == 0
                            )
                            continue;

                        var attributeC = attribute.Cast<DisplayNameAttribute>().Single();

                        if (attributeC == null)
                            continue;

                        string displayName = attributeC.DisplayName;

                        lstCamposDePara.Add(new LogCamposAlterados()
                        {
                            Campo = displayName,
                            De = propValueOld,
                            Para = propValueNew
                        });
                    }
                }

                return lstCamposDePara;
            }
            catch (Exception ex)
            {

                throw new Exception("Erro gerar campos para log" + ex.Message);
            }

        }

        #endregion
    }
}
