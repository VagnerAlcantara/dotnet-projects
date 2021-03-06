﻿using BookStore.Context;
using BookStore.Domain;
using BookStore.Repositories.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BookStore.Repositories
{
    public class AuthorRepository : IAuthorRepository
    {
        private BookStoreDataContext _db;

        public AuthorRepository(BookStoreDataContext db)
        {
            _db = db;
        }

        public bool Create(Autor autor)
        {
            try
            {
                _db.Autores.Add(autor);
                _db.SaveChanges();

                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public void Delete(int id)
        {
            var autor = _db.Autores.Find(id);
            _db.Autores.Remove(autor);
            _db.SaveChanges();
        }

        public List<Autor> Get()
        {
            return _db.Autores.ToList();

        }

        public Autor Get(int id)
        {
            return _db.Autores.Find(id);

        }

        public List<Autor> Get(string name)
        {
            return _db.Autores.Where(x => x.Nome.Contains(name)).ToList();

        }

        public bool Update(Autor autor)
        {
            try
            {
                _db.Entry<Autor>(autor).State = System.Data.Entity.EntityState.Modified;
                _db.SaveChanges();

                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}