﻿using System;
using System.Collections.Generic;
using System.Linq;
using YouLearn.Domain.Entities;
using YouLearn.Domain.Interfaces.Repositories;
using YouLearn.Infra.Persistence.EF;

namespace YouLearn.Infra.Persistence.Repositories
{
    public class RepositoryPlayList : IRepositoryPlayList
    {
        private readonly YouLearnContext _context;

        public RepositoryPlayList(YouLearnContext context)
        {
            _context = context;
        }

        public PlayList Adicionar(PlayList playList)
        {
            _context.PlayLists.Add(playList);

            return playList;
        }

        public void Excluir(Guid id)
        {
            var playList = _context.PlayLists.Find(id);

            _context.PlayLists.Remove(playList);
        }

        public IEnumerable<PlayList> Listar(Guid idUsuario)
        {
            return _context.PlayLists.Where(x => x.Usuario.Id == idUsuario).ToList();

        }

        public PlayList Obter(Guid id)
        {
            return _context.PlayLists.Find(id);
        }
    }
}
