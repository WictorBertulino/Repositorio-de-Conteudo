using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlataformaDeEnsino.Application.AppServices.Interfaces;
using PlataformaDeEnsino.Core.Entities;
using PlataformaDeEnsino.Identity.Models;
using PlataformaDeEnsino.Presenter.ViewModels;

namespace PlataformaDeEnsino.Presenter.Controllers
{
    public class AlunoCoordenadorController : Controller
    {
        private readonly IMapper _mapper;
        private Coordenador _coordenadorUsuario;
        private readonly ICoordenadorAppService _coordenadorAppService;
        private readonly IAlunoAppService _alunoAppService;
        private readonly ICursoAppService _cursoAppService;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AlunoCoordenadorController(IMapper mapper, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IAlunoAppService alunoAppService, ICursoAppService cursoAppService, ICoordenadorAppService coordenadorAppService)
        {
            _mapper = mapper;
            _alunoAppService = alunoAppService;
            _cursoAppService = cursoAppService;
            _userManager = userManager;
            _signInManager = signInManager;
            _coordenadorAppService = coordenadorAppService;
        }

        private Coordenador CoodernadorUsuario()
        {
            return _coordenadorAppService.ConsultarPeloCpf(User.Identity.Name);
        }

        [HttpGet("AlunoCoordenador")]
        public IActionResult AlunoCoordenador()
        {
            _coordenadorUsuario = CoodernadorUsuario();
            ViewBag.UserName = _coordenadorUsuario.NomeDoCoordenador + " " + _coordenadorUsuario.SobrenomeDoCoordenador;
            var alunosViewModel = _mapper.Map<IEnumerable<Aluno>, IEnumerable<AlunoViewModel>>(_alunoAppService.SelecionarAlunosPeloCurso(1));
            return View(alunosViewModel);
        }

        [HttpGet("NovoAluno")]
        public ViewResult NovoAluno()
        {
            return View();
        }

        [HttpPost("NovoAluno")]
        public async Task<IActionResult> NovoAluno(AlunoViewModel alunoViewModel)
        {

            if (ModelState.IsValid)
            {
                var aluno = _mapper.Map<AlunoViewModel, Aluno>(alunoViewModel);
                _alunoAppService.Inserir(aluno);
                var user = new AppUser { UserName = alunoViewModel.CpfDoAluno, Email = alunoViewModel.EmailDoAluno };
                var resultCreate = await _userManager.CreateAsync(user, alunoViewModel.CpfDoAluno);
                if (resultCreate.Succeeded)
                {
                    var resultRole = await _userManager.AddToRoleAsync(user, alunoViewModel.Role);
                    if (resultRole.Succeeded)
                    {
                        return Redirect("AlunoCoordenador");
                    }
                }
            }
            return View(alunoViewModel);
        }

        [HttpGet("VisualizarAluno")]
        public async Task<IActionResult> VisualizarAluno(int? IdDoAluno, int? IdDoCurso)
        {
            if (IdDoCurso != null)
            {
                var idDoCurso = Convert.ToInt32(IdDoCurso);
                ViewBag.Curso = _mapper.Map<Curso, CursoViewModel>(_cursoAppService.ConsultarPeloId(idDoCurso));
            }
            if (IdDoAluno != null)
            {
                var idDoAluno = Convert.ToInt32(IdDoAluno);
                var alunoViewModel = _mapper.Map<Aluno, AlunoViewModel>(_alunoAppService.ConsultarPeloId(idDoAluno));
                alunoViewModel.Usuario = await _userManager.FindByNameAsync(alunoViewModel.CpfDoAluno);
                return View(alunoViewModel);
            }
            return View();
        }

        [HttpGet("EditarAluno")]
        public async Task<ViewResult> EditarAluno(int? IdDoAluno, string IdDoUsuario)
        {
            if (IdDoAluno != null)
            {
                var idDoAluno = Convert.ToInt32(IdDoAluno);
                var alunoViewModel = _mapper.Map<Aluno, AlunoViewModel>(_alunoAppService.ConsultarPeloId(idDoAluno));
                alunoViewModel.IdDoUsuario = IdDoUsuario;
                alunoViewModel.Usuario = await _userManager.FindByIdAsync(IdDoUsuario);
                return View(alunoViewModel);
            }
            return View();
        }
        [HttpPost("EditarAluno")]
        public async Task<IActionResult> EditarAluno(AlunoViewModel alunoViewModel)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _userManager.FindByIdAsync(alunoViewModel.IdDoUsuario);
                if (usuario != null)
                {
                    var userChangePassword = await _userManager.ChangePasswordAsync(usuario, usuario.UserName, alunoViewModel.CpfDoAluno);
                    if (userChangePassword.Succeeded)
                    {
                        usuario.UserName = alunoViewModel.CpfDoAluno;
                        usuario.Email = alunoViewModel.EmailDoAluno;
                        var resultadoDaAtualizacaoDoUsuario = await _userManager.UpdateAsync(usuario);
                        if (resultadoDaAtualizacaoDoUsuario.Succeeded)
                        {
                            var aluno = _mapper.Map<AlunoViewModel, Aluno>(alunoViewModel);
                            _alunoAppService.Atualizar(aluno);
                            return Redirect("VisualizarAluno?IdDoAluno=" + alunoViewModel.IdDoAluno + "&IdDoCurso=" + alunoViewModel.IdDoCurso);
                        }
                    }
                }
            }
            return View(alunoViewModel);
        }
        [HttpGet("DeletarAluno")]
        public async Task<IActionResult> DeletarAluno(int IdDoAluno)
        {
            var aluno = _alunoAppService.ConsultarPeloId(IdDoAluno);
            var usuario = await _userManager.FindByNameAsync(aluno.CpfDoAluno);
            var deletandoUsuario = await _userManager.DeleteAsync(usuario);
            if (deletandoUsuario.Succeeded)
            {
                _alunoAppService.Deletar(aluno.IdDoAluno);
            }
            return Redirect("AlunoCoordenador");
        }
    }
}