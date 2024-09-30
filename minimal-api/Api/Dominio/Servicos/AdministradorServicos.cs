using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;
using minimal_api.Infraestrutura;

namespace minimal_api.Dominio.Servicos
{
    public class AdministradorServicos : IAdminisatradorServicos
    {
 
        private readonly DbContexto _contexto;

        public AdministradorServicos(DbContexto contexto){
            _contexto = contexto;
        }


        public Administrador? BuscarPorId(int? id)
        {
            return _contexto.administradores.Where(v => v.Id == id).FirstOrDefault();
        }

        public Administrador Incluir(Administrador administrador)
        {
           _contexto.administradores.Add(administrador);
           _contexto.SaveChanges();

           return administrador;
        }

        public Administrador? Login(LoginDTO loginDTO)
        {
            var adm = _contexto.administradores.Where(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();
            return adm;
        }

        public List<Administrador> Todos(int? pagina)
        {
            var query = _contexto.administradores.AsQueryable();
            int itensPorPaginas = 10;

            if(pagina != null){

                query = query.Skip(((int)pagina - 1) * itensPorPaginas).Take(itensPorPaginas);
            }


            return query.ToList();
        }
    }
}